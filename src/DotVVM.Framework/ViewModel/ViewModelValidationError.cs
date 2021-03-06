using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DotVVM.Framework.ViewModel
{
    public class ViewModelValidationError
    {
        /// <summary>
        /// Contains path that can be evaluated on the client side.
        /// E.g.: Product().Suppliers()[2].Name
        /// </summary>
        [JsonProperty("propertyPath")]
        public string PropertyPath { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}