using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotVVM.Framework.Binding;
using DotVVM.Framework.Exceptions;
using DotVVM.Framework.Hosting;
using DotVVM.Framework.Parser;
using DotVVM.Framework.Runtime;
using Newtonsoft.Json;

namespace DotVVM.Framework.Controls
{
    /// <summary>
    /// Allows the user to upload one or multiple files asynchronously.
    /// </summary>
    [ControlMarkupOptions(AllowContent = false)]
    public class FileUpload : HtmlGenericControl
    {

        /// <summary>
        /// Gets or sets whether the user can select multiple files at once.
        /// </summary>
        [MarkupOptions(AllowBinding = false)]
        public bool AllowMultipleFiles
        {
            get { return (bool)GetValue(AllowMultipleFilesProperty); }
            set { SetValue(AllowMultipleFilesProperty, value); }
        }
        public static readonly DotvvmProperty AllowMultipleFilesProperty
            = DotvvmProperty.Register<bool, FileUpload>(p => p.AllowMultipleFiles, true);

        /// <summary>
        /// Gets or sets a collection of uploaded files.
        /// </summary>
        [MarkupOptions(AllowHardCodedValue = false)]
        public UploadedFilesCollection UploadedFiles
        {
            get { return (UploadedFilesCollection) GetValue(UploadedFilesProperty); }
            set { SetValue(UploadedFilesProperty, value); }
        }
        public static readonly DotvvmProperty UploadedFilesProperty
            = DotvvmProperty.Register<UploadedFilesCollection, FileUpload>(p => p.UploadedFiles, null);


        /// <summary>
        /// Gets or sets a command that is triggered when the upload is complete.
        /// </summary>
        [MarkupOptions(AllowHardCodedValue = false)]
        public Command UploadCompleted
        {
            get { return (Command)GetValue(UploadCompletedProperty); }
            set { SetValue(UploadCompletedProperty, value); }
        }
        public static readonly DotvvmProperty UploadCompletedProperty
            = DotvvmProperty.Register<Command, FileUpload>(p => p.UploadCompleted, null);


        /// <summary>
        /// Gets or sets the text on the upload button. The default value is "Upload".
        /// </summary>
        [Localizable(true)]
        public string UploadButtonText
        {
            get { return (string)GetValue(UploadButtonTextProperty); }
            set { SetValue(UploadButtonTextProperty, value); }
        }
        public static readonly DotvvmProperty UploadButtonTextProperty
            = DotvvmProperty.Register<string, FileUpload>(c => c.UploadButtonText, Resources.Controls.FileUpload_UploadButtonText, isValueInherited: true);

        /// <summary>
        /// Gets or sets the text on the indicator showing number of files. The defaule value is "{0} files". The number of files will be substituted for the "{0}" placeholder.
        /// </summary>
        [Localizable(true)]
        public string NumberOfFilesIndicatorText
        {
            get { return (string)GetValue(NumberOfFilesIndicatorTextProperty); }
            set { SetValue(NumberOfFilesIndicatorTextProperty, value); }
        }
        public static readonly DotvvmProperty NumberOfFilesIndicatorTextProperty
            = DotvvmProperty.Register<string, FileUpload>(c => c.NumberOfFilesIndicatorText, Resources.Controls.FileUpload_NumberOfFilesText, isValueInherited: true);

        /// <summary>
        /// Gets or sets the text that appears when there is an error during the upload.
        /// </summary>
        [Localizable(true)]
        public string UploadErrorMessageText
        {
            get { return (string)GetValue(UploadErrorMessageTextProperty); }
            set { SetValue(UploadErrorMessageTextProperty, value); }
        }
        public static readonly DotvvmProperty UploadErrorMessageTextProperty
            = DotvvmProperty.Register<string, FileUpload>(c => c.UploadErrorMessageText, Resources.Controls.FileUpload_UploadErrorMessageText, isValueInherited: true);

        /// <summary>
        /// Gets or sets the text that appears when all files are uploaded successfully.
        /// </summary>
        [Localizable(true)]
        public string SuccessMessageText
        {
            get { return (string)GetValue(SuccessMessageTextProperty); }
            set { SetValue(SuccessMessageTextProperty, value); }
        }
        public static readonly DotvvmProperty SuccessMessageTextProperty
            = DotvvmProperty.Register<string, FileUpload>(c => c.SuccessMessageText, Resources.Controls.FileUpload_SuccessMessageText, isValueInherited: true);




        public FileUpload() : base("div")
        {
        }

        protected internal override void OnInit(IDotvvmRequestContext context)
        {
            EnsureControlHasId();

            base.OnInit(context);
        }

        internal override void OnPreRenderComplete(IDotvvmRequestContext context)
        {
            context.ResourceManager.AddRequiredResource(Constants.DotvvmFileUploadCssResourceName);

            base.OnPreRenderComplete(context);
        }

        protected override void AddAttributesToRender(IHtmlWriter writer, RenderContext context)
        {
            writer.AddKnockoutDataBind("with", this, UploadedFilesProperty, () =>
            {
                throw new DotvvmControlException(this, "The UploadedFiles property of the FileUpload control must be bound!"); 
            });
            writer.AddAttribute("class", "dot-upload", true);

            var uploadCompletedBinding = GetCommandBinding(UploadCompletedProperty);
            if (uploadCompletedBinding != null)
            {
                writer.AddAttribute("data-upload-completed", KnockoutHelper.GenerateClientPostBackScript(nameof(UploadCompleted), uploadCompletedBinding, context, this, true, null));
            }

            base.AddAttributesToRender(writer, context);
        }

        protected override void RenderContents(IHtmlWriter writer, RenderContext context)
        {
            // render iframe
            writer.AddAttribute("class", "dot-upload-iframe");
            writer.AddAttribute("src", "~/" + Constants.FileUploadHandlerMatchUrl + (AllowMultipleFiles ? "?multiple=true" : ""));
            writer.RenderBeginTag("iframe");
            writer.RenderEndTag();

            // render upload button
            writer.AddAttribute("class", "dot-upload-button");
            writer.AddKnockoutDataBind("visible", "!IsBusy()");
            writer.RenderBeginTag("span");
            writer.AddAttribute("href", "#");
            writer.AddAttribute("onclick", "dotvvm.fileUpload.showUploadDialog(this); return false;");
            writer.RenderBeginTag("a");
            writer.WriteUnencodedText(UploadButtonText);
            writer.RenderEndTag();
            writer.RenderEndTag();

            // render upload files
            writer.AddAttribute("class", "dot-upload-files");
            writer.AddKnockoutDataBind("html", $"dotvvm.globalize.format({JsonConvert.SerializeObject(NumberOfFilesIndicatorText)}, Files().length)");
            writer.RenderBeginTag("span");
            writer.RenderEndTag();

            // render progress wrapper
            writer.AddKnockoutDataBind("visible", "IsBusy");
            writer.AddAttribute("class", "dot-upload-progress-wrapper");
            writer.RenderBeginTag("span");
            writer.AddAttribute("class", "dot-upload-progress");
            writer.AddKnockoutDataBind("style", "{ 'width': (Progress() == -1 ? '50' : Progress()) + '%' }");
            writer.RenderBeginTag("span");
            writer.RenderEndTag();
            writer.RenderEndTag();

            // render result
            writer.AddAttribute("class", "dot-upload-result");
            writer.AddKnockoutDataBind("html", $"Error() ? {JsonConvert.SerializeObject(UploadErrorMessageText)} : {JsonConvert.SerializeObject(SuccessMessageText)}");
            writer.AddKnockoutDataBind("attr", "{ title: Error }");
            writer.AddKnockoutDataBind("css", "{ 'dot-upload-result-success': !Error(), 'dot-upload-result-error': Error }");
            writer.AddKnockoutDataBind("visible", "!IsBusy() && Files().length > 0");
            writer.RenderBeginTag("span");
            writer.RenderEndTag();

            base.RenderContents(writer, context);
        }
    }
}
