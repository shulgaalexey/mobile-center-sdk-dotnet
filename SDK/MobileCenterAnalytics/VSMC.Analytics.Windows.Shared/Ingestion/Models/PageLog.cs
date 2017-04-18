// Copyright (c) Microsoft Corporation.  All rights reserved.
using System.Collections.Generic;
using Microsoft.Azure.Mobile.Ingestion.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.Mobile.Analytics.Ingestion.Models
{
    /// <summary>
    /// Page view log (as in screens or activities).
    /// </summary>
    [JsonObject(JsonIdentifier)]
    public partial class PageLog : LogWithProperties
    {
        /// <summary>
        /// Initializes a new instance of the PageLog class.
        /// </summary>
        public PageLog() { }

        internal const string JsonIdentifier = "page";

        /// <summary>
        /// Initializes a new instance of the PageLog class.
        /// </summary>
        /// <param name="toffset">Corresponds to the number of milliseconds
        /// elapsed between the time the request is sent and the time the log
        /// is emitted.</param>
        /// <param name="name">Name of the page.
        /// </param>
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
        public PageLog(long toffset, Mobile.Ingestion.Models.Device device, string name, System.Guid? sid = default(System.Guid?), IDictionary<string, string> properties = default(IDictionary<string, string>))
            : base(toffset, device, sid, properties)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets name of the page.
        ///
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public override void Validate()
        {
            base.Validate();
            if (Name == null)
            {
                throw new Microsoft.Rest.ValidationException(Microsoft.Rest.ValidationRules.CannotBeNull, "Name");
            }
        }
    }
}

