﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DotVVM.Framework.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Controls {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Controls() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DotVVM.Framework.Resources.Controls", typeof(Controls).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The control &lt;{0}:{1}&gt; could not be resolved! Make sure that the tagPrefix is registered in DotvvmConfiguration.Markup.Controls collection!.
        /// </summary>
        internal static string ControlResolver_ControlNotFound {
            get {
                return ResourceManager.GetString("ControlResolver_ControlNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} files.
        /// </summary>
        internal static string FileUpload_NumberOfFilesText {
            get {
                return ResourceManager.GetString("FileUpload_NumberOfFilesText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The files were uploaded successfully..
        /// </summary>
        internal static string FileUpload_SuccessMessageText {
            get {
                return ResourceManager.GetString("FileUpload_SuccessMessageText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Upload.
        /// </summary>
        internal static string FileUpload_UploadButtonText {
            get {
                return ResourceManager.GetString("FileUpload_UploadButtonText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error occured..
        /// </summary>
        internal static string FileUpload_UploadErrorMessageText {
            get {
                return ResourceManager.GetString("FileUpload_UploadErrorMessageText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Markup controls must derive from DotvvmMarkupControl class!.
        /// </summary>
        internal static string ViewCompiler_MarkupControlMustDeriveFromDotvvmMarkupControl {
            get {
                return ResourceManager.GetString("ViewCompiler_MarkupControlMustDeriveFromDotvvmMarkupControl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type &apos;{0}&apos; specified in basetype directive was not found!.
        /// </summary>
        internal static string ViewCompiler_TypeSpecifiedInBaseTypeDirectiveNotFound {
            get {
                return ResourceManager.GetString("ViewCompiler_TypeSpecifiedInBaseTypeDirectiveNotFound", resourceCulture);
            }
        }
    }
}
