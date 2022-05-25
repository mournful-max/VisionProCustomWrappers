using System.Collections.Generic;

using Cognex.VisionPro;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ImageProcessing;


namespace VisionProCustomWrappers
{
    public class VisionProPerformAlignment : VisionProImageSaver
    {
        private readonly HashSet<string> _ToolsName;
        private readonly HashSet<string> _OutputsName;

        public double? TranslationX { private set; get; }
        public double? TranslationY { private set; get; }

        public VisionProPerformAlignment(CogToolBlockEditV2 cogToolBlockEditV2 = null, string recipeName = VISION_TOOLS_DEFAULT_RECIPE_NAME) : base(cogToolBlockEditV2, recipeName)
        {
            _ToolsName = new HashSet<string>();
            _ToolsName.Add(_ACQ_FIFO_TOOL_NAME_SNAPSHOTS);
            _ToolsName.Add(_IMAGE_CONVERT_TOOL_NAME_INTENSITY);
            _ToolsName.Add(_CALIB_TOOL_NAME_CALIBRATION);
            _ToolsName.Add(_PM_ALIGN_TOOL_NAME_FIND_FIDUSIAL);

            _OutputsName = new HashSet<string>();
            _OutputsName.Add(_OUTPUT_NAME_TRANSLATION_X);
            _OutputsName.Add(_OUTPUT_NAME_TRANSLATION_Y);
        }

        public override void LoadToolBlock()
        {
            base.LoadToolBlock();
            VerifyOrInitialize();
        }

        public override void SaveToolBlock()
        {
            VerifyOrInitialize();
            base.SaveToolBlock();
        }

        public bool VerifyOrInitialize()
        {
            bool isVerified = ToolBlockAvailable;

            if (isVerified)
            {
                foreach (string toolName in _ToolsName)
                {
                    if (!_CogToolBlock.Tools.Contains(toolName))
                    {
                        isVerified = false;
                        break;
                    }
                }
                foreach (string outputName in _OutputsName)
                {
                    if (!_CogToolBlock.Outputs.Contains(outputName))
                    {
                        isVerified = false;
                        break;
                    }
                }
            }
            if (!isVerified)
            {
                CreateEmptyToolBlock();
                // ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ----

                CogAcqFifoTool cogAcqFifoTool = new CogAcqFifoTool() { Name = _ACQ_FIFO_TOOL_NAME_SNAPSHOTS };

                _CogToolBlock.Tools.Add(cogAcqFifoTool);
                // ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ----

                CogImageConvertTool cogImageConvertTool = new CogImageConvertTool() { Name = _IMAGE_CONVERT_TOOL_NAME_INTENSITY };

                cogImageConvertTool.DataBindings.Add(_TOOL_TERMINAL_INPUT_NAME_IMAGE, cogAcqFifoTool, _TOOL_TERMINAL_OUTPUT_NAME_IMAGE);

                _CogToolBlock.Tools.Add(cogImageConvertTool);
                // ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ----

                CogCalibNPointToNPointTool cogCalibNPointToNPointTool = new CogCalibNPointToNPointTool() { Name = _CALIB_TOOL_NAME_CALIBRATION };

                cogCalibNPointToNPointTool.DataBindings.Add(_TOOL_TERMINAL_INPUT_NAME_IMAGE, cogImageConvertTool, _TOOL_TERMINAL_OUTPUT_NAME_IMAGE);

                _CogToolBlock.Tools.Add(cogCalibNPointToNPointTool);
                // ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ----

                CogPMAlignTool cogPMAlignTool = new CogPMAlignTool() { Name = _PM_ALIGN_TOOL_NAME_FIND_FIDUSIAL };

                cogPMAlignTool.DataBindings.Add(_TOOL_TERMINAL_INPUT_NAME_IMAGE, cogCalibNPointToNPointTool, _TOOL_TERMINAL_OUTPUT_NAME_IMAGE);

                _CogToolBlock.Tools.Add(cogPMAlignTool);
                // ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ----

                CogToolBlockTerminal outputTranslationX = new CogToolBlockTerminal(_OUTPUT_NAME_TRANSLATION_X, typeof(double));
                CogToolBlockTerminal outputTranslationY = new CogToolBlockTerminal(_OUTPUT_NAME_TRANSLATION_Y, typeof(double));

                _CogToolBlock.Outputs.Add(outputTranslationX);
                _CogToolBlock.Outputs.Add(outputTranslationY);
                // ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ----

                _CogToolBlock.Outputs.DataBindings.Add(
                "Item[\"" + outputTranslationX.ID + "\"].Value.(System.Double)", cogPMAlignTool, "Results.Item[0].GetPose()." + _OUTPUT_NAME_TRANSLATION_X);
                _CogToolBlock.Outputs.DataBindings.Add(
                "Item[\"" + outputTranslationY.ID + "\"].Value.(System.Double)", cogPMAlignTool, "Results.Item[0].GetPose()." + _OUTPUT_NAME_TRANSLATION_Y);
            }
            return isVerified;
        }

        private void LastRunOutputsExtracting()
        {
            TranslationX = GetOutput(_OUTPUT_NAME_TRANSLATION_X) as double?;
            TranslationY = GetOutput(_OUTPUT_NAME_TRANSLATION_Y) as double?;
        }

        public override bool Run()
        {
            bool result = base.Run();

            LastRunOutputsExtracting();

            return result;
        }


        private const string _TOOL_TERMINAL_INPUT_NAME_IMAGE    = "InputImage";
        private const string _TOOL_TERMINAL_OUTPUT_NAME_IMAGE   = "OutputImage";

        private const string _ACQ_FIFO_TOOL_NAME_SNAPSHOTS      = "Snapshots";
        private const string _IMAGE_CONVERT_TOOL_NAME_INTENSITY = "Intensity";
        private const string _CALIB_TOOL_NAME_CALIBRATION       = "Calibration";
        private const string _PM_ALIGN_TOOL_NAME_FIND_FIDUSIAL  = "FindFidusial";

        private const string _OUTPUT_NAME_TRANSLATION_X         = "TranslationX";
        private const string _OUTPUT_NAME_TRANSLATION_Y         = "TranslationY";
    }
}
