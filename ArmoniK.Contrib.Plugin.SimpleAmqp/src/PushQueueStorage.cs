// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-2023. All rights reserved.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amqp;
using Amqp.Framing;

using ArmoniK.Core.Base;
using ArmoniK.Core.Base.DataStructures;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace ArmoniK.Contrib.Plugin.SimpleAmqp;

/// <summary>
///   Policy for creating a <see cref="Session" /> for the <see cref="ObjectPool{Session}" />
/// </summary>
internal sealed class SessionPooledObjectPolicy : IPooledObjectPolicy<Session>
{
  private readonly IConnectionAmqp connectionAmqp_;

  /// <summary>
  ///   Initializes a <see cref="SessionPooledObjectPolicy" />
  /// </summary>
  /// <param name="connectionAmqp">AMQP connection that will be used to create new sessions</param>
  public SessionPooledObjectPolicy(IConnectionAmqp connectionAmqp)
    => connectionAmqp_ = connectionAmqp;

  /// <inheritdoc />
  public Session Create()
    => new(connectionAmqp_.Connection);

  /// <inheritdoc />
  public bool Return(Session obj)
    => !obj.IsClosed;
}

public class PushQueueStorage : QueueStorage, IPushQueueStorage
{
  private const int MaxInternalQueuePriority = 10;

  private readonly ILogger<PushQueueStorage> logger_;
  private readonly ObjectPool<Session>       sessionPool_;


  public PushQueueStorage(Amqp                      options,
                          IConnectionAmqp           connectionAmqp,
                          ILogger<PushQueueStorage> logger)
    : base(options,
           connectionAmqp)
  {
    logger_      = logger;
    sessionPool_ = new DefaultObjectPool<Session>(new SessionPooledObjectPolicy(ConnectionAmqp));
  }

  /// <inheritdoc />
  public async Task PushMessagesAsync(IEnumerable<MessageData> messages,
                                      string                   partitionId,
                                      CancellationToken        cancellationToken = default)
  {
    if (!IsInitialized)
    {
      throw new InvalidOperationException($"{nameof(PushQueueStorage)} should be initialized before calling this method.");
    }

    var session = sessionPool_.Get();
    try
    {
      var sender = new SenderLink(session,
                                  $"{partitionId}###SenderLink",
                                  $"{partitionId}###q");

      await Task.WhenAll(messages.Select(msgData => sender.SendAsync(new Message(Encoding.UTF8.GetBytes(msgData.TaskId))
                                                                     {
                                                                       Header = new Header
                                                                                {
                                                                                  Priority = (byte)msgData.Options.Priority,
                                                                                },
                                                                       Properties = new Properties(),
                                                                     })))
                .ConfigureAwait(false);

      await sender.CloseAsync()
                  .ConfigureAwait(false);
    }
    finally
    {
      sessionPool_.Return(session);
    }
  }
}
