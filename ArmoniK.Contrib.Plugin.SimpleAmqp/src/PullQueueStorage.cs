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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amqp;

using ArmoniK.Core.Base;
using ArmoniK.Core.Base.DataStructures;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ArmoniK.Contrib.Plugin.SimpleAmqp;

public class PullQueueStorage : QueueStorage, IPullQueueStorage
{
  private readonly ILogger<PullQueueStorage> logger_;

  private IReceiverLink? receiver_;
  private ISenderLink?   sender_;

  public PullQueueStorage(Amqp                      options,
                          IConnectionAmqp           connectionAmqp,
                          ILogger<PullQueueStorage> logger)
    : base(options,
           connectionAmqp)
  {
    if (string.IsNullOrEmpty(options.PartitionId))
    {
      throw new ArgumentOutOfRangeException(nameof(options),
                                            $"{nameof(Amqp.PartitionId)} is not defined.");
    }

    logger_   = logger;
    sender_   = null;
    receiver_ = null;
  }

  public override Task<HealthCheckResult> Check(HealthCheckTag tag)
    => ConnectionAmqp.Check(tag);

  public override async Task Init(CancellationToken cancellationToken)
  {
    if (!IsInitialized)
    {
      await ConnectionAmqp.Init(cancellationToken)
                          .ConfigureAwait(false);
      var session = new Session(ConnectionAmqp.Connection);

      sender_ = new SenderLink(session,
                               $"{Options.PartitionId}###SenderLink",
                               $"{Options.PartitionId}###q");

      receiver_ = new ReceiverLink(session,
                                   $"{Options.PartitionId}###ReceiverLink",
                                   $"{Options.PartitionId}###q");

      /* linkCredit_: the maximum number of messages the remote peer can send to the receiver.
       * With the goal of minimizing/deactivating prefetching, a value of 1 gave us the desired behavior.
       * We pick a default value of 2 to have "some cache". */
      receiver_.SetCredit(Options.LinkCredit,
                          true);
      IsInitialized = true;
    }
  }

  /// <inheritdoc />
  public async IAsyncEnumerable<IQueueMessageHandler> PullMessagesAsync(int                                        nbMessages,
                                                                        [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var nbPulledMessage = 0;

    if (!IsInitialized)
    {
      throw new InvalidOperationException($"{nameof(PullQueueStorage)} should be initialized before calling this method.");
    }

    while (nbPulledMessage < nbMessages)
    {
      cancellationToken.ThrowIfCancellationRequested();
      // receiver_ and sender_ are guaranteed to not be null here because we already checked IsInitialized
      var message = await receiver_!.ReceiveAsync(TimeSpan.FromMilliseconds(100))
                                    .ConfigureAwait(false);

      if (message is null)
      {
        logger_.LogTrace("Message is null for receiver");
        continue;
      }

      nbPulledMessage++;

      yield return new QueueMessageHandler(message,
                                           sender_!,
                                           receiver_!,
                                           Encoding.UTF8.GetString(message.Body as byte[] ?? throw new InvalidOperationException("Error while deserializing message")),
                                           logger_,
                                           cancellationToken);
    }
  }
}
