using System;
using System.IO;
using System.Collections.Generic;

using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;


namespace VisionProCustomWrappers
{
    public abstract class VisionProBase
    {
        private static VisionProLiberator _VisionProLiberator;

        private   CogToolBlock __CogToolBlock;
        protected CogToolBlock  _CogToolBlock
        {
            get
            {
                return __CogToolBlock;
            }
            set
            {
                if (value != null)
                {
                    if (value is CogToolBlock)
                    {
                        ToolBlockAvailable = true;
                    }
                    else
                    {
                        throw new ArgumentException("Assigned value is not CogToolBlock or its inheritor.");
                    }
                }
                else
                {
                    ToolBlockAvailable = false;
                }
                __CogToolBlock = value;
            }
        }

        protected CogToolBlockEditV2 _CogToolBlockEditV2;

        private string _CogToolBlockFullPath;
        public  string  CogToolBlockFullPath
        {
            get
            {
                return _CogToolBlockFullPath;
            }
            set
            {
                string potentialNewCogToolBlockFullPath = Path.GetFullPath(value);

                if (potentialNewCogToolBlockFullPath.EndsWith(VISION_TOOLS_EXTENSION))
                {
                    _CogToolBlockFullPath = potentialNewCogToolBlockFullPath;
                }
                else
                {
                    _CogToolBlockFullPath = potentialNewCogToolBlockFullPath + VISION_TOOLS_EXTENSION;
                }
            }
        }

        public HashSet<CogRecordDisplay> RecordDisplays
        {
            private set; get;
        }

        public bool ToolBlockAvailable
        {
            private set; get;
        }

        public VisionProBase(CogToolBlockEditV2 cogToolBlockEditV2 = null, string recipeName = VISION_TOOLS_DEFAULT_RECIPE_NAME)
        {
            if (_VisionProLiberator == null)
            {
                _VisionProLiberator = VisionProLiberator.Instance;
            }
            _CogToolBlockEditV2  = cogToolBlockEditV2;
            CogToolBlockFullPath = Path.Combine(_VisionToolsDefaultFileDirectoryPath, recipeName, GetType().Name) + VISION_TOOLS_EXTENSION;
            RecordDisplays       = new HashSet<CogRecordDisplay>();
        }

        protected void CreateEmptyToolBlock()
        {
            _CogToolBlock = new CogToolBlock();
            _CogToolBlock.AbortRunOnToolFailure = false;

            if (_CogToolBlockEditV2 != null)
            {
                _CogToolBlockEditV2.Subject = _CogToolBlock;
            }
        }

        public virtual void LoadToolBlock()
        {
            if (File.Exists(CogToolBlockFullPath))
            {
                _CogToolBlock = CogSerializer.LoadObjectFromFile(CogToolBlockFullPath) as CogToolBlock;

                if (_CogToolBlockEditV2 != null)
                {
                    _CogToolBlockEditV2.Subject = _CogToolBlock;
                }
            }
            else
            {
                CreateEmptyToolBlock();
            }
        }

        public virtual void SaveToolBlock()
        {
            if (!Directory.Exists(Path.GetDirectoryName(CogToolBlockFullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CogToolBlockFullPath));
            }
            if (_CogToolBlockEditV2 != null && _CogToolBlockEditV2.Subject != null)
            {
                _CogToolBlock = _CogToolBlockEditV2.Subject;
            }
            else if (_CogToolBlock == null)
            {
                CreateEmptyToolBlock();
            }
            CogSerializer.SaveObjectToFile(_CogToolBlock, CogToolBlockFullPath);
        }

        protected virtual void RefreshRecordDisplays()
        {
            if (!ToolBlockAvailable)
            {
                throw _ToolBlockNotAvailableException;
            }
            foreach (CogRecordDisplay cogRecordDisplay in RecordDisplays)
            {
                if (cogRecordDisplay != null)
                {
                    cogRecordDisplay.StaticGraphics.Clear();
                    cogRecordDisplay.InteractiveGraphics.Clear();

                    ICogRecords records = _CogToolBlock.CreateLastRunRecord().SubRecords;
                    if (records.Count > 0)
                    {
                        cogRecordDisplay.Record  = records[records.Count - 1];
                        cogRecordDisplay.AutoFit = true;
                        cogRecordDisplay.Fit();
                    }
                }
            }
        }

        public virtual bool Run()
        {
            if (ToolBlockAvailable)
            {
                _CogToolBlock.Run();
                RefreshRecordDisplays();
                return _CogToolBlock.RunStatus.Result == CogToolResultConstants.Accept;
            }
            else
            {
                throw _ToolBlockNotAvailableException;
            }
        }

        public virtual void SetInput(string inputName, object inputValue)
        {
            if (String.IsNullOrWhiteSpace(inputName))
            {
                throw new ArgumentException("The name of Input Block Terminal is incorrect.");
            }
            if (!ToolBlockAvailable)
            {
                throw _ToolBlockNotAvailableException;
            }
            if (!_CogToolBlock.Inputs.Contains(inputName))
            {
                throw new KeyNotFoundException("The name of Input Block Terminal cannot be found.");
            }
            _CogToolBlock.Inputs[inputName].Value = inputValue;
        }

        public virtual object GetOutput(string outputName)
        {
            if (String.IsNullOrWhiteSpace(outputName))
            {
                throw new ArgumentException("The name of Output Block Terminal is incorrect.");
            }
            if (!ToolBlockAvailable)
            {
                throw _ToolBlockNotAvailableException;
            }
            if (!_CogToolBlock.Outputs.Contains(outputName))
            {
                throw new KeyNotFoundException("The name of Output Block Terminal cannot be found.");
            }
            return _CogToolBlock.Outputs[outputName].Value;
        }


        public static void DisconnectAllCogFrameGrabbers() => VisionProLiberator.DisconnectAllCogFrameGrabbers();


        private sealed class VisionProLiberator
        {
            private VisionProLiberator() {}

            private static VisionProLiberator _Instance;

            public  static VisionProLiberator  Instance
            {
                get
                {
                    if (_Instance == null)
                    {
                        _Instance = new VisionProLiberator();
                    }
                    return _Instance;
                }
            }

            private static bool _DisconnectAllCogFrameGrabbersFired;

            public static void DisconnectAllCogFrameGrabbers()
            {
                CogFrameGrabbers cogFrameGrabbers = new CogFrameGrabbers();
                foreach (ICogFrameGrabber cogFrameGrabber in cogFrameGrabbers)
                {
                    cogFrameGrabber.Disconnect(false);
                }
                _DisconnectAllCogFrameGrabbersFired = true;
            }

            ~VisionProLiberator()
            {
                if (!_DisconnectAllCogFrameGrabbersFired)
                {
                    try { DisconnectAllCogFrameGrabbers(); } catch { }
                }
            }
        }

        protected static readonly MemberAccessException _ToolBlockNotAvailableException      = new MemberAccessException("Tool Block has never been loaded or created.");
        protected static readonly string                _VisionToolsDefaultFileDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VisionProToolBlocks");

        public const string VISION_TOOLS_EXTENSION           = ".vpp";
        public const string VISION_TOOLS_DEFAULT_RECIPE_NAME = "DefaultRecipe";
    }
}
