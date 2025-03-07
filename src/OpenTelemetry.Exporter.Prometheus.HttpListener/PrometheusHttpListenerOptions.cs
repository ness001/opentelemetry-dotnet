// <copyright file="PrometheusHttpListenerOptions.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Exporter.Prometheus.HttpListener
{
    /// <summary>
    /// <see cref="PrometheusHttpListener"/> options.
    /// </summary>
    public class PrometheusHttpListenerOptions
    {
        private IReadOnlyCollection<string> prefixes = new string[] { "http://localhost:9464/" };

        /// <summary>
        /// Gets or sets the prefixes to use for the http listener.
        /// Default value: http://localhost:9464/.
        /// </summary>
        public IReadOnlyCollection<string> Prefixes
        {
            get => this.prefixes;
            set
            {
                Guard.ThrowIfNull(value);

                foreach (string inputUri in value)
                {
                    if (!(Uri.TryCreate(inputUri, UriKind.Absolute, out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
                    {
                        throw new ArgumentException(
                            "Prometheus HttpListener prefix path should be a valid URI with http/https scheme.",
                            nameof(this.prefixes));
                    }
                }

                this.prefixes = value;
            }
        }
    }
}
