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

using Microsoft.Extensions.Configuration;

namespace ArmoniK.Contrib.Plugin.SimpleAmqp;

/// <summary>
///   Extends the functionality of the <see cref="IConfiguration" />
/// </summary>
public static class ConfigurationExt
{
  /// <summary>
  ///   Configure an object with the given configuration.
  /// </summary>
  /// <typeparam name="T">Type of the options class</typeparam>
  /// <param name="configuration">Configurations used to populate the class</param>
  /// <param name="key">Path to the Object in the configuration</param>
  /// <returns>
  ///   The initialized object
  /// </returns>
  /// <exception cref="InvalidOperationException">the <paramref name="key" /> is not found in the configurations.</exception>
  public static T GetRequiredValue<T>(this IConfiguration configuration,
                                      string              key)
    => configuration.GetRequiredSection(key)
                    .Get<T>() ?? throw new InvalidOperationException($"{key} not found");
}
