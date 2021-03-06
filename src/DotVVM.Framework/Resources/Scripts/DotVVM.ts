/// <reference path="typings/knockout/knockout.d.ts" />
/// <reference path="typings/knockout.mapper/knockout.mapper.d.ts" />
/// <reference path="typings/globalize/globalize.d.ts" />
interface IRenderedResourceList {
    [name: string]: string;
}

interface IDotvvmPostbackScriptFunction {
    (pageArea: string, sender: HTMLElement, pathFragments: string[], controlId: string, useWindowSetTimeout: boolean, validationTarget: string, context: any, handlers: IDotvvmPostBackHandlerConfiguration[]): void
}

interface IDotvvmExtensions {
}

interface IDotvvmViewModel {
    viewModel?;
    renderedResources?: string[];
    url?: string;
    virtualDirectory?: string;
    validationRules?: any;
}

interface IDotvvmViewModels {
    [name: string]: IDotvvmViewModel
}

class DotVVM {
    private postBackCounter = 0;
    private resourceSigns: { [name: string]: boolean } = {}
    private isViewModelUpdating: boolean = true;
    private viewModelObservables: {
        [name: string]: KnockoutObservable<IDotvvmViewModel>;
    } = {};

    public isSpaReady = ko.observable(false);
    public viewModels: IDotvvmViewModels = {};
    public culture: string;
    public serialization = new DotvvmSerialization();
    public postBackHandlers = new DotvvmPostBackHandlers();
    public events = new DotvvmEvents();
    public globalize = new DotvvmGlobalize();
    public evaluator = new DotvvmEvaluator();
    public domUtils = new DotvvmDomUtils();
    public fileUpload = new DotvvmFileUpload();
    public validation: DotvvmValidation;
    public extensions: IDotvvmExtensions = {};

    public init(viewModelName: string, culture: string): void {
        this.addKnockoutBindingHandlers();

        // load the viewmodel
        var thisViewModel = this.viewModels[viewModelName] = JSON.parse((<HTMLInputElement>document.getElementById("__dot_viewmodel_" + viewModelName)).value);
        if (thisViewModel.renderedResources) {
            thisViewModel.renderedResources.forEach(r => this.resourceSigns[r] = true);
        }
        var viewModel = thisViewModel.viewModel = this.serialization.deserialize(this.viewModels[viewModelName].viewModel, {}, true);

        // initialize services
        this.culture = culture;
        this.validation = new DotvvmValidation(this);

        // wrap it in the observable
        this.viewModelObservables[viewModelName] = ko.observable(viewModel);
        ko.applyBindings(this.viewModelObservables[viewModelName], document.documentElement);

        // trigger the init event
        this.events.init.trigger(new DotvvmEventArgs(viewModel));
        this.isViewModelUpdating = false;

        // handle SPA requests
        var spaPlaceHolder = this.getSpaPlaceHolder();
        if (spaPlaceHolder) {
            this.domUtils.attachEvent(window, "hashchange", () => this.handleHashChange(viewModelName, spaPlaceHolder));
            this.handleHashChange(viewModelName, spaPlaceHolder);
        }

        // persist the viewmodel in the hidden field so the Back button will work correctly
        this.domUtils.attachEvent(window, "beforeunload", e => {
            this.persistViewModel(viewModelName);
        });
    }

    private handleHashChange(viewModelName: string, spaPlaceHolder: HTMLElement) {
        if (document.location.hash.indexOf("#!/") === 0) {
            this.navigateCore(viewModelName, document.location.hash.substring(2));
        } else {
            // redirect to the default URL
            var url = spaPlaceHolder.getAttribute("data-dot-spacontentplaceholder-defaultroute");
            if (url) {
                document.location.hash = "#!/" + url;
            } else {
                this.isSpaReady(true);
            }
        }
    }

    // binding helpers
    private postbackScript(bindingId: string): IDotvvmPostbackScriptFunction {
        return (pageArea, sender, pathFragments, controlId, useWindowSetTimeout, validationTarget, context, handlers) => {
            this.postBack(pageArea, sender, pathFragments, bindingId, controlId, useWindowSetTimeout, validationTarget, context, handlers);
        }
    }

    private persistViewModel(viewModelName: string) {
        var viewModel = this.viewModels[viewModelName];
        var persistedViewModel = {};
        for (var p in viewModel) {
            if (viewModel.hasOwnProperty(p)) {
                persistedViewModel[p] = viewModel[p];
            }
        }
        persistedViewModel["viewModel"] = this.serialization.serialize(persistedViewModel["viewModel"], { serializeAll: true });
        (<HTMLInputElement>document.getElementById("__dot_viewmodel_" + viewModelName)).value = JSON.stringify(persistedViewModel);
    }

    private backUpPostBackConter(): number {
        this.postBackCounter++;
        return this.postBackCounter;
    }

    private isPostBackStillActive(currentPostBackCounter: number): boolean {
        return this.postBackCounter === currentPostBackCounter;
    }

    public staticCommandPostback(viewModelName: string, sender: HTMLElement, command: string, args: any[], callback = _ => { }, errorCallback = (xhr: XMLHttpRequest) => { }) {
        if (this.isPostBackProhibited(sender)) return;

        // TODO: events for static command postback

        // prevent double postbacks
        var currentPostBackCounter = this.backUpPostBackConter();

        var data = this.serialization.serialize({
            "args": args,
            "command": command,
            "$csrfToken": this.viewModels[viewModelName].viewModel.$csrfToken
        });

        this.postJSON(this.viewModels[viewModelName].url, "POST", ko.toJSON(data), response => {
            if (!this.isPostBackStillActive(currentPostBackCounter)) return;
            callback(JSON.parse(response.responseText));
        }, errorCallback,
            xhr => {
                xhr.setRequestHeader("X-PostbackType", "StaticCommand");
            });
    }

    public postBack(viewModelName: string, sender: HTMLElement, path: string[], command: string, controlUniqueId: string, useWindowSetTimeout: boolean, validationTargetPath?: any, context?: any, handlers?: IDotvvmPostBackHandlerConfiguration[]): IDotvvmPromise<DotvvmAfterPostBackEventArgs> {
        if (this.isPostBackProhibited(sender)) return;

        var promise = new DotvvmPromise<DotvvmAfterPostBackEventArgs>();

        if (useWindowSetTimeout) {
            window.setTimeout(() => promise.chainFrom(this.postBack(viewModelName, sender, path, command, controlUniqueId, false, validationTargetPath, context, handlers)), 0);
            return promise;
        }

        // apply postback handlers
        if (handlers && handlers.length > 0) {
            var handler = this.postBackHandlers[handlers[0].name];
            var options = this.evaluator.evaluateOnViewModel(ko.contextFor(sender), "(" + handlers[0].options.toString() + ")()");
            var handlerInstance = handler(options);
            handlerInstance.execute(
                () => promise.chainFrom(this.postBack(viewModelName, sender, path, command, controlUniqueId, false, validationTargetPath, context, handlers.slice(1))),
                sender
            );
            return promise;
        }

        var viewModel = this.viewModels[viewModelName].viewModel;

        // prevent double postbacks
        var currentPostBackCounter = this.backUpPostBackConter();

        // trigger beforePostback event
        var beforePostbackArgs = new DotvvmBeforePostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath);
        this.events.beforePostback.trigger(beforePostbackArgs);
        if (beforePostbackArgs.cancel) {
            // trigger afterPostback event
            var afterPostBackArgsCanceled = new DotvvmAfterPostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath, null);
            afterPostBackArgsCanceled.wasInterrupted = true;
            this.events.afterPostback.trigger(afterPostBackArgsCanceled);
            return;
        }

        // perform the postback
        if (!context) {
            context = ko.contextFor(sender);
        }
        this.updateDynamicPathFragments(context, path);
        var data = {
            viewModel: this.serialization.serialize(viewModel, { pathMatcher(val) { return context && val == context.$data } }),
            currentPath: path,
            command: command,
            controlUniqueId: controlUniqueId,
            validationTargetPath: validationTargetPath || null,
            renderedResources: this.viewModels[viewModelName].renderedResources
        };
        this.postJSON(this.viewModels[viewModelName].url, "POST", ko.toJSON(data), result => {
            // if another postback has already been passed, don't do anything
            if (!this.isPostBackStillActive(currentPostBackCounter)) {
                var afterPostBackArgsCanceled = new DotvvmAfterPostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath, null);
                afterPostBackArgsCanceled.wasInterrupted = true;
                this.events.afterPostback.trigger(afterPostBackArgsCanceled);
                promise.reject("postback collision");
                return;
            }

            var resultObject = JSON.parse(result.responseText);
            if (!resultObject.viewModel && resultObject.viewModelDiff) {
                // TODO: patch (~deserialize) it to ko.observable viewModel
                this.isViewModelUpdating = true;
                resultObject.viewModel = this.patch(data.viewModel, resultObject.viewModelDiff);
            }

            this.loadResourceList(resultObject.resources, () => {
                var isSuccess = false;
                if (resultObject.action === "successfulCommand") {
                    this.isViewModelUpdating = true;

                    // remove updated controls
                    var updatedControls = this.cleanUpdatedControls(resultObject);

                    // update the viewmodel
                    if (resultObject.viewModel) {
                        this.serialization.deserialize(resultObject.viewModel, this.viewModels[viewModelName].viewModel);
                    }
                    isSuccess = true;

                    // remove updated controls which were previously hidden
                    this.cleanUpdatedControls(resultObject, updatedControls);

                    // add updated controls
                    this.restoreUpdatedControls(resultObject, updatedControls, true);
                    this.isViewModelUpdating = false;
                } else if (resultObject.action === "redirect") {
                    // redirect
                    this.handleRedirect(resultObject, viewModelName);
                    return;
                }

                // trigger afterPostback event
                var afterPostBackArgs = new DotvvmAfterPostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath, resultObject);
                promise.resolve(afterPostBackArgs);
                this.events.afterPostback.trigger(afterPostBackArgs);
                if (!isSuccess && !afterPostBackArgs.isHandled) {
                    throw "Invalid response from server!";
                }
            });
        }, xhr => {
            // if another postback has already been passed, don't do anything
            if (!this.isPostBackStillActive(currentPostBackCounter)) return;

            // execute error handlers
            var errArgs = new DotvvmErrorEventArgs(viewModel, xhr);
            promise.reject(errArgs);
            this.events.error.trigger(errArgs);
            if (!errArgs.handled) {
                alert(xhr.responseText);
            }
        });
        return promise;
    }

    private loadResourceList(resources: IRenderedResourceList, callback: () => void) {
        var html = "";
        for (var name in resources) {
            if (this.resourceSigns[name]) continue;
            this.resourceSigns[name] = true;
            html += resources[name] + " ";
        }
        if (html.trim() == "") {
            setTimeout(callback, 4);
            return;
        }
        else {
            var tmp = document.createElement("div");
            tmp.innerHTML = html;
            var elements: HTMLElement[] = [];
            for (var i = 0; i < tmp.children.length; i++) {
                elements.push(<HTMLElement>tmp.children.item(i));
            }
            this.loadResourceElements(elements, 0, callback);
        }
    }

    private loadResourceElements(elements: HTMLElement[], offset: number, callback: () => void) {
        if (offset >= elements.length) {
            callback();
            return;
        }
        var el = <any>elements[offset];
        var waitForScriptLoaded = false;
        if (el.tagName.toLowerCase() == "script") {
            // create the script element
            var script = document.createElement("script");
            if (el.src) {
                script.src = el.src;
                waitForScriptLoaded = true;
            }
            if (el.type) {
                script.type = el.type;
            }
            if (el.text) {
                script.text = el.text;
            }
            el = script;
        }
        else if (el.tagName.toLowerCase() == "link") {
            // create link
            var link = document.createElement("link");
            if (el.href) {
                link.href = el.href;
            }
            if (el.rel) {
                link.rel = el.rel;
            }
            if (el.type) {
                link.type = el.type;
            }
            el = link;
        }

        // load next script when this is finished
        if (waitForScriptLoaded) {
            el.onload = () => this.loadResourceElements(elements, offset + 1, callback);
        }
        document.head.appendChild(el);
        if (!waitForScriptLoaded) {
            this.loadResourceElements(elements, offset + 1, callback);
        }
    }

    private getSpaPlaceHolder(): HTMLElement {
        var elements = document.getElementsByName("__dot_SpaContentPlaceHolder");
        if (elements.length == 1) {
            return <HTMLElement>elements[0];
        }
        return null;
    }

    private navigateCore(viewModelName: string, url: string) {
        var viewModel = this.viewModels[viewModelName].viewModel;

        // prevent double postbacks
        var currentPostBackCounter = this.backUpPostBackConter();

        // trigger spaNavigating event
        var spaNavigatingArgs = new DotvvmSpaNavigatingEventArgs(viewModel, viewModelName, url);
        this.events.spaNavigating.trigger(spaNavigatingArgs);
        if (spaNavigatingArgs.cancel) {
            return;
        }

        // add virtual directory prefix
        var fullUrl = this.addLeadingSlash(this.concatUrl(this.viewModels[viewModelName].virtualDirectory || "", url));

        // find SPA placeholder
        var spaPlaceHolder = this.getSpaPlaceHolder();
        if (!spaPlaceHolder) {
            document.location.href = fullUrl;
            return;
        }

        // send the request
        var spaPlaceHolderUniqueId = spaPlaceHolder.attributes["data-dot-spacontentplaceholder"].value;
        this.getJSON(fullUrl, "GET", spaPlaceHolderUniqueId, result => {
            // if another postback has already been passed, don't do anything
            if (!this.isPostBackStillActive(currentPostBackCounter)) return;

            var resultObject = JSON.parse(result.responseText);
            this.loadResourceList(resultObject.resources, () => {
                var isSuccess = false;
                if (resultObject.action === "successfulCommand" || !resultObject.action) {
                    this.isViewModelUpdating = true;

                    // remove updated controls
                    var updatedControls = this.cleanUpdatedControls(resultObject);

                    // update the viewmodel
                    this.viewModels[viewModelName] = {};
                    for (var p in resultObject) {
                        if (resultObject.hasOwnProperty(p)) {
                            this.viewModels[viewModelName][p] = resultObject[p];
                        }
                    }

                    this.serialization.deserialize(resultObject.viewModel, this.viewModels[viewModelName].viewModel);
                    isSuccess = true;

                    // add updated controls
                    this.viewModelObservables[viewModelName](this.viewModels[viewModelName].viewModel);
                    this.restoreUpdatedControls(resultObject, updatedControls, true);

                    this.isSpaReady(true);
                    this.isViewModelUpdating = false;
                } else if (resultObject.action === "redirect") {
                    this.handleRedirect(resultObject, viewModelName, true);
                    return;
                }

                // trigger spaNavigated event
                var spaNavigatedArgs = new DotvvmSpaNavigatedEventArgs(viewModel, viewModelName, resultObject);
                this.events.spaNavigated.trigger(spaNavigatedArgs);
                if (!isSuccess && !spaNavigatedArgs.isHandled) {
                    throw "Invalid response from server!";
                }
            });
        }, xhr => {
            // if another postback has already been passed, don't do anything
            if (!this.isPostBackStillActive(currentPostBackCounter)) return;

            // execute error handlers
            var errArgs = new DotvvmErrorEventArgs(viewModel, xhr, true);
            this.events.error.trigger(errArgs);
            if (!errArgs.handled) {
                alert(xhr.responseText);
            }
        });
    }

    private handleRedirect(resultObject: any, viewModelName: string, replace?: boolean) {
        if (resultObject.replace != null) replace = resultObject.replace;
        var url;
        // redirect
        if (this.getSpaPlaceHolder() && resultObject.url.indexOf("//") < 0) {
            // relative URL - keep in SPA mode, but remove the virtual directory
            url = "#!" + this.removeVirtualDirectoryFromUrl(resultObject.url, viewModelName);
        } else {
            // absolute URL - load the URL
            url = resultObject.url;
        }
        if (replace) location.replace(url);
        else location.href = url;
    }

    private removeVirtualDirectoryFromUrl(url: string, viewModelName: string) {
        var virtualDirectory = "/" + this.viewModels[viewModelName].virtualDirectory;
        if (url.indexOf(virtualDirectory) == 0) {
            return this.addLeadingSlash(url.substring(virtualDirectory.length));
        } else {
            return url;
        }
    }

    private addLeadingSlash(url: string) {
        if (url.length > 0 && url.substring(0, 1) != "/") {
            return "/" + url;
        }
        return url;
    }

    private concatUrl(url1: string, url2: string) {
        if (url1.length > 0 && url1.substring(url1.length - 1) == "/") {
            url1 = url1.substring(0, url1.length - 1);
        }
        return url1 + this.addLeadingSlash(url2);
    }

    public patch(source: any, patch: any): any {
        if (source instanceof Array && patch instanceof Array) {
            return patch.map((val, i) =>
                this.patch(source[i], val));
        }
        else if (source instanceof Array || patch instanceof Array)
            return patch;
        else if (typeof source == "object" && typeof patch == "object") {
            for (var p in patch) {
                if (patch[p] == null) source[p] = null;
                else if (source[p] == null) source[p] = patch[p];
                else source[p] = this.patch(source[p], patch[p]);
            }
        }
        else return patch;

        return source;
    }

    private updateDynamicPathFragments(context: any, path: string[]): void {
        for (var i = path.length - 1; i >= 0; i--) {
            if (path[i].indexOf("[$index]") >= 0) {
                path[i] = path[i].replace("[$index]", "[" + context.$index() + "]");
            }
            context = context.$parentContext;
        }
    }

    private postJSON(url: string, method: string, postData: any, success: (request: XMLHttpRequest) => void, error: (response: XMLHttpRequest) => void, preprocessRequest = (xhr: XMLHttpRequest) => { }) {
        var xhr = this.getXHR();
        xhr.open(method, url, true);
        xhr.setRequestHeader("Content-Type", "application/json");
        preprocessRequest(xhr);
        xhr.onreadystatechange = () => {
            if (xhr.readyState != 4) return;
            if (xhr.status < 400) {
                success(xhr);
            } else {
                error(xhr);
            }
        };
        xhr.send(postData);
    }

    private getJSON(url: string, method: string, spaPlaceHolderUniqueId: string, success: (request: XMLHttpRequest) => void, error: (response: XMLHttpRequest) => void) {
        var xhr = this.getXHR();
        xhr.open(method, url, true);
        xhr.setRequestHeader("Content-Type", "application/json");
        xhr.setRequestHeader("X-DotVVM-SpaContentPlaceHolder", spaPlaceHolderUniqueId);
        xhr.onreadystatechange = () => {
            if (xhr.readyState != 4) return;
            if (xhr.status < 400) {
                success(xhr);
            } else {
                error(xhr);
            }
        };
        xhr.send();
    }

    public getXHR(): XMLHttpRequest {
        return XMLHttpRequest ? new XMLHttpRequest() : <XMLHttpRequest>new ActiveXObject("Microsoft.XMLHTTP");
    }

    private cleanUpdatedControls(resultObject: any, updatedControls: any = {}) {
        for (var id in resultObject.updatedControls) {
            if (resultObject.updatedControls.hasOwnProperty(id)) {
                var control = document.getElementById(id);
                if (control) {
                    var nextSibling = control.nextSibling;
                    var parent = control.parentNode;
                    ko.removeNode(control);
                    updatedControls[id] = { control: control, nextSibling: nextSibling, parent: parent };
                }
            }
        }
        return updatedControls;
    }

    private restoreUpdatedControls(resultObject: any, updatedControls: any, applyBindingsOnEachControl: boolean) {
        for (var id in resultObject.updatedControls) {
            if (resultObject.updatedControls.hasOwnProperty(id)) {
                var updatedControl = updatedControls[id];
                if (updatedControl) {
                    if (updatedControl.nextSibling) {
                        updatedControl.parent.insertBefore(updatedControl.control, updatedControl.nextSibling);
                    } else {
                        updatedControl.parent.appendChild(updatedControl.control);
                    }
                    updatedControl.control.outerHTML = resultObject.updatedControls[id];
                }
            }
        }

        if (applyBindingsOnEachControl) {
            window.setTimeout(() => {
                for (var id in resultObject.updatedControls) {
                    var updatedControl = document.getElementById(id);
                    if (updatedControl) {
                        ko.applyBindings(ko.dataFor(updatedControl.parentNode), updatedControl);
                    }
                }
            }, 0);
        }
    }

    public unwrapArrayExtension(array: any): any {
        return ko.unwrap(ko.unwrap(array));
    }
    public buildRouteUrl(routePath: string, params: any): string {
        return routePath.replace(/\{[^\}]+\??\}/g, s => {
            let paramName = s.substring(1, s.length - 1).toLowerCase();
            if (paramName && paramName.length > 0 && paramName.substring(paramName.length - 1) === "?") {
                paramName = paramName.substring(0, paramName.length - 1);
            }
            return ko.unwrap(params[paramName]) || "";
        });
    }

    private isPostBackProhibited(element: HTMLElement) {
        if (element.tagName.toLowerCase() === "a" && element.getAttribute("disabled")) {
            return true;
        }
        return false;
    }

    private addKnockoutBindingHandlers() {
        ko.virtualElements.allowedBindings["withControlProperties"] = true;
        ko.bindingHandlers["withControlProperties"] = {
            init: (element, valueAccessor, allBindings, viewModel, bindingContext) => {
                var value = valueAccessor();
                for (var prop in value) {
                    if (!ko.isObservable(value[prop])) {
                        value[prop] = ko.observable(value[prop]);
                    }
                }
                var innerBindingContext = bindingContext.extend({ $control: value });
                ko.applyBindingsToDescendants(innerBindingContext, element);

                return { controlsDescendantBindings: true }; // do not apply binding again
            }
        };

        ko.bindingHandlers['dotvvmEnable'] = {
            'update': (element, valueAccessor) => {
                var value = ko.utils.unwrapObservable(valueAccessor());
                if (value && element.disabled) {
                    element.disabled = false;
                    element.removeAttribute("disabled");
                } else if ((!value) && (!element.disabled)) {
                    element.disabled = true;
                    element.setAttribute("disabled", "disabled");
                }
            }
        };

        ko.bindingHandlers["dotvvmUpdateProgressVisible"] = {
            init(element: any, valueAccessor: () => any, allBindingsAccessor: KnockoutAllBindingsAccessor, viewModel: any, bindingContext: KnockoutBindingContext) {
                element.style.display = "none";
                dotvvm.events.beforePostback.subscribe(e => {
                    element.style.display = "";
                });
                dotvvm.events.spaNavigating.subscribe(e => {
                    element.style.display = "";
                });
                dotvvm.events.afterPostback.subscribe(e => {
                    element.style.display = "none";
                });
                dotvvm.events.spaNavigated.subscribe(e => {
                    element.style.display = "none";
                });
                dotvvm.events.error.subscribe(e => {
                    element.style.display = "none";
                });
            }
        };
        ko.bindingHandlers['dotvvm-textbox-text'] = {
            init(element: any, valueAccessor: () => any, allBindingsAccessor: KnockoutAllBindingsAccessor, viewModel: any, bindingContext: KnockoutBindingContext) {
                dotvvm.domUtils.attachEvent(element, "blur", () => {
                    var format = (element.attributes["data-dotvvm-format"] || { value: "" }).value;
                    var obs = valueAccessor();
                    if ((element.attributes["data-dotvvm-value-type"] || { value: "" }).value === "datetime") {
                        var result = dotvvm.globalize.parseDate(element.value, format);
                        if (!result) {
                            element.attributes["data-dotvvm-value-type-valid"] = false;
                        } else {
                            element.attributes["data-dotvvm-value-type-valid"] = true;
                        }
                        if (result) {
                            obs(dotvvm.serialization.serializeDate(result, false));
                        }
                        else {
                            obs(null);
                        }
                    } else {
                        var newValue = dotvvm.globalize.parseNumber(element.value);
                        if (!isNaN(newValue)) {
                            element.attributes["data-dotvvm-value-type-valid"] = true;
                            if (obs() === newValue) {
                                if ((<KnockoutObservable<number>>obs).valueHasMutated) {
                                    (<KnockoutObservable<number>>obs).valueHasMutated();
                                } else {
                                    (<KnockoutObservable<number>>obs).notifySubscribers();
                                }
                            } else {
                                obs(newValue);
                            }
                        } else {
                            element.attributes["data-dotvvm-value-type-valid"] = false;
                            obs(null);
                        }
                    }
                });
            },
            update(element: any, valueAccessor: () => any, allBindingsAccessor: KnockoutAllBindingsAccessor, viewModel: any, bindingContext: KnockoutBindingContext) {
                var value = ko.unwrap(valueAccessor());
                if (element.attributes["data-dotvvm-value-type-valid"] != false) {
                    var format = (element.attributes["data-dotvvm-format"] || { value: "" }).value;
                    if (format) {
                        element.value = dotvvm.globalize.formatString(format, value);
                    } else {
                        element.value = value;
                    }
                }
            }
        };
    }
}