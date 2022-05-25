using System;
using System.IO;
using System.Collections.Generic;

using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;


namespace VisionProCustomWrappers
{
    public class VisionProImageSaver : VisionProBase
    {
        private string _SavedImagesDirectory;
        public  string  SavedImagesDirectory
        {
            set
            {
                _SavedImagesDirectory = Path.GetFullPath(value);
            }
            get
            {
                return _SavedImagesDirectory;
            }
        }

        private string _SavedImagesByDaysCurrentDirectory
        {
            get
            {
                return Path.Combine(SavedImagesDirectory, DateTime.Now.ToString(_DATE_FORMAT));
            }
        }

        private string GetPathToSaveImage(int imageId = 0) => Path.Combine(_SavedImagesByDaysCurrentDirectory,
                                                                           DateTime.Now.ToString(_DATETIME_FORMAT) + _IMAGE_ID_SEPARATOR + imageId.ToString() + _BMP_EXTENSION);

        public HashSet<string> SaveImagesOutputNames
        {
            private set; get;
        }

        public bool ImageSaving { get; set; }

        public bool CreateImageSavingOutputIfAbsent { get; set; }

        public VisionProImageSaver(CogToolBlockEditV2 cogToolBlockEditV2 = null, string recipeName = VISION_TOOLS_DEFAULT_RECIPE_NAME) : base(cogToolBlockEditV2, recipeName)
        {
            SavedImagesDirectory  = Path.Combine(Path.GetDirectoryName(CogToolBlockFullPath),
                                                 Path.GetFileNameWithoutExtension(CogToolBlockFullPath) + _DEFAULT_SAVED_IMAGES_DIRECTORY_POSTFIX);
            SaveImagesOutputNames = new HashSet<string>();
            SaveImagesOutputNames.Add(_DEFAULT_IMAGE_SAVE_OUTPUT_NAME);
            CreateImageSavingOutputIfAbsent = true;
        }

        public int SaveLastRunImages()
        {
            if (!ToolBlockAvailable)
            {
                throw _ToolBlockNotAvailableException;
            }
            if (!Directory.Exists(_SavedImagesByDaysCurrentDirectory))
            {
                Directory.CreateDirectory(_SavedImagesByDaysCurrentDirectory);
            }
            int imageCounter = 1;

            foreach (string outputName in SaveImagesOutputNames)
            {
                if (CreateImageSavingOutputIfAbsent && !_CogToolBlock.Outputs.Contains(outputName))
                {
                    _CogToolBlock.Outputs.Add(new CogToolBlockTerminal(outputName, typeof(ICogImage)));
                }
                object cogImage = GetOutput(outputName);

                if (cogImage == null)
                {
                    continue;
                }
                else if (cogImage is CogImage24PlanarColor)
                {
                    CogImage24PlanarColor cogImage24PlanarColor = cogImage as CogImage24PlanarColor;
                    cogImage24PlanarColor.ToBitmap().Save(GetPathToSaveImage(imageCounter), System.Drawing.Imaging.ImageFormat.Bmp);
                }
                else if (cogImage is CogImage8Grey)
                {
                    CogImage8Grey cogImage8Grey = cogImage as CogImage8Grey;
                    cogImage8Grey.ToBitmap().Save(GetPathToSaveImage(imageCounter), System.Drawing.Imaging.ImageFormat.Bmp);
                }
                else
                {
                    throw new FormatException("Not supported image format. Implemented only for: CogImage24PlanarColor, CogImage8Grey.");
                }
                imageCounter += 1;
            }
            return imageCounter - 1;
        }

        public override bool Run()
        {
            bool result = base.Run();

            if (ImageSaving)
            {
                SaveLastRunImages();
            }
            return result;
        }


        private const string _DEFAULT_SAVED_IMAGES_DIRECTORY_POSTFIX = "_SavedImages";
        private const string _DEFAULT_IMAGE_SAVE_OUTPUT_NAME         = "ImageToSave";

        private const string _IMAGE_ID_SEPARATOR = "_#_";

        private const string _DATETIME_FORMAT = "yyyy_MM_dd_T_hh_mm_ss_ff_tt";
        private const string _DATE_FORMAT     = "yyyy_MM_dd";
        private const string _BMP_EXTENSION   = ".bmp";
    }
}
