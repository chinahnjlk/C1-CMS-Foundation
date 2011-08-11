using System;
using System.Collections.Generic;
using System.Xml;
using Composite.C1Console.Forms.Foundation.FormTreeCompiler;
using Composite.C1Console.Forms.Foundation.FormTreeCompiler.CompilePhases;
using Composite.C1Console.Forms.Foundation.FormTreeCompiler.CompileTreeNodes;
using Composite.Core.ResourceSystem;
using Composite.Core.ResourceSystem.Icons;
using Composite.Data.Validation.ClientValidationRules;


namespace Composite.C1Console.Forms
{
    /// <summary>    
    /// </summary>
    /// <exclude />
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
    public sealed class FormTreeCompiler
    {
        private CompileContext _context;
        private Dictionary<string, object> _bindingObjects;

        private IUiControl _uiControl = null;
        private CompileTreeNode _rootCompilerNode = null;

        private string _label;
        private string _iconHandle;


        /// <exclude />
        public void Compile(XmlReader reader, IFormChannelIdentifier channel, Dictionary<string, object> bindingObjects)
        {
            Compile(reader, channel, bindingObjects, false, "", null);
        }


        /// <exclude />
        public void Compile(XmlReader reader, IFormChannelIdentifier channel, Dictionary<string, object> bindingObjects, Dictionary<string, List<ClientValidationRule>> bindingsValidationRules)
        {
            Compile(reader, channel, bindingObjects, false, "", bindingsValidationRules);
        }


        /// <exclude />
        public void Compile(XmlReader reader, IFormChannelIdentifier channel, Dictionary<string, object> bindingObjects, bool withDebug)
        {
            Compile(reader, channel, bindingObjects, withDebug, "", null);
        }


        /// <exclude />
        public void Compile(XmlReader reader, IFormChannelIdentifier channel, Dictionary<string, object> bindingObjects, bool withDebug, Dictionary<string, List<ClientValidationRule>> bindingsValidationRules)
        {
            Compile(reader, channel, bindingObjects, withDebug, "", bindingsValidationRules);
        }


        /// <exclude />
        public void Compile(XmlReader reader, IFormChannelIdentifier channel, Dictionary<string, object> bindingObjects, bool withDebug, string customControlIdPrefix)
        {
            Compile(reader, channel, bindingObjects, withDebug, customControlIdPrefix, null);
        }


        /// <exclude />
        public void Compile(XmlReader reader, IFormChannelIdentifier channel, Dictionary<string, object> bindingObjects, bool withDebug, string customControlIdPrefix, Dictionary<string, List<ClientValidationRule>> bindingsValidationRules)
        {
            _bindingObjects = bindingObjects;

            _context = new CompileContext();
            _context.BindingObjects = bindingObjects;
            _context.BindingsValidationRules = bindingsValidationRules;
            _context.CurrentChannel = channel;
            _context.CustomControlIdPrefix = customControlIdPrefix;

            BuildFromXmlPhase buildPhase = new BuildFromXmlPhase(reader);
            _rootCompilerNode = buildPhase.BuildTree();

            UpdateXmlInformationPhase updateInfo = new UpdateXmlInformationPhase();
            updateInfo.UpdateInformation(_rootCompilerNode);

            CreateProducersPhase createProducers = new CreateProducersPhase(_context);
            createProducers.CreateProducers(_rootCompilerNode);            
          
            EvaluatePropertiesPhase evaluateProperties = new EvaluatePropertiesPhase(_context, withDebug);
            evaluateProperties.Evaluate(_rootCompilerNode);

            ExtractUiArtifactsPhase extractUiArtifacts = new ExtractUiArtifactsPhase();

            extractUiArtifacts.ExtractUiArtifacts(_rootCompilerNode, out _uiControl, out _label, out _iconHandle);
        }

        /// <exclude />
        public void SaveControlProperties()
        {
            SaveAndValidateControlProperties();
        }

        internal Dictionary<string, Exception> SaveAndValidateControlProperties()
        {
            _uiControl.BindStateToControlProperties();

            var bindingErrors = new Dictionary<string, Exception>();

            foreach (CompileContext.IRebinding rd in _context.Rebindings)
            {
                rd.Rebind(_bindingObjects, bindingErrors);
            }

            return bindingErrors;
        }


        /// <exclude />
        public IUiControl UiControl
        {
            get { return _uiControl; }
        }


        /// <exclude />
        public string Label
        {
            get 
            {
                if (string.IsNullOrEmpty(_label) == false)
                {
                    return _label;
                }
                else
                {
                    return _uiControl.Label;
                }
            }
        }


        /// <exclude />
        public ResourceHandle Icon
        {
            get
            {
                if (string.IsNullOrEmpty(_iconHandle) == false)
                {
                    if (_iconHandle.IndexOf(',') == -1)
                    {
                        return new ResourceHandle(BuildInIconProviderName.ProviderName, _iconHandle.Trim());
                    }
                    else
                    {
                        string[] resourceParts = _iconHandle.Split(',');
                        if (resourceParts.Length != 2) throw new InvalidOperationException( string.Format( "Invalid icon resource name '{0}'. Only one comma expected.", _iconHandle ));

                        return new ResourceHandle(resourceParts[0].Trim(), resourceParts[1].Trim());
                    }
                }
                else
                {
                    return null;
                }
            }
        }


        /// <exclude />
        public CompileTreeNode RootCompileTreeNode
        {
            get { return _rootCompilerNode; }
        }
    }
}
