// Copyright (c) Microsoft Corporation.  All rights reserved.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Azure.Mobile.Ingestion.Models
{
    public abstract class LogWithProperties : Log
    {
        /// <summary>
        /// Initializes a new instance of the LogWithProperties class.
        /// </summary>
        protected LogWithProperties() { }

        /// <summary>
        /// Initializes a new instance of the LogWithProperties class.
        /// </summary>
        /// <param name="toffset">Corresponds to the number of milliseconds
        /// elapsed between the time the request is sent and the time the log
        /// is emitted.</param>
        /// <param name="sid">When tracking an analytics session, logs can be
        /// part of the session by specifying this identifier.
        /// This attribute is optional, a missing value means the session
        /// tracking is disabled (like when using only error reporting
        /// feature).
        /// Concrete types like StartSessionLog or PageLog are always part of a
        /// session and always include this identifier.
        /// </param>
        /// <param name="properties">Additional key/value pair parameters.
        /// </param>
        ///<param name="device">Description of the device emitting the log</param>
        protected LogWithProperties(long toffset, Device device, System.Guid? sid = default(System.Guid?), IDictionary<string, string> properties = default(IDictionary<string, string>))
            : base(toffset, device, sid)
        {
            Properties = properties;
        }

        /// <summary>
        /// Gets or sets additional key/value pair parameters.
        ///
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public IDictionary<string, string> Properties { get; set; }
    }
}

