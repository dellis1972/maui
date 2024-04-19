using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Maui.Resizetizer
{
    public class CalculateResizetizeOutputs : MauiAsyncTask, ILogger
    {
        [Required]
        public string PlatformType { get; set; } = "android";

        [Required]
        public string IntermediateOutputPath { get; set; }

        public string InputsFile { get; set; }

        public ITaskItem[] Images { get; set; }

        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        ConcurrentBag<ResizedImageInfo> resizedImages = new ConcurrentBag<ResizedImageInfo>();

        public override System.Threading.Tasks.Task ExecuteAsync()
        {
            var inputImages = ResizeImageInfo.Parse(Images);
            var images = RemoveDuplicates(inputImages);

            var dpis = DpiPath.GetDpis(PlatformType);
            var originalScaleDpi = DpiPath.GetOriginal(PlatformType);

            foreach (var image in images)
            {
                if (image.IsAppIcon)
                {
                    CalculateAppIcons(image, resizedImages, dpis);
                    continue;
                }
                if (image.Resize)
                {
                    foreach (var dpi in dpis)
			        {
                        resizedImages.Add(new ResizedImageInfo { Dpi = dpi, Filename = InputsFile });
                    }
                    continue;
                }
                resizedImages.Add(new ResizedImageInfo { Dpi = originalScaleDpi, Filename = InputsFile });
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }

        void CalculateAppIcons(ResizeImageInfo image, ConcurrentBag<ResizedImageInfo> resizedImages, DpiPath[] dpis)
        {
            var appIconName = image.OutputName;
            var appIconDpis = DpiPath.GetAppIconDpis(PlatformType, appIconName);
            switch (PlatformType)
            {
                case "android":
                    appIconName = appIconName.ToLowerInvariant();
                    var adaptiveIconGen = new AndroidAdaptiveIconGenerator(image, appIconName, IntermediateOutputPath, this, generateImages: false);
                    var iconsGenerated = adaptiveIconGen.Generate();
                    foreach (var iconGenerated in iconsGenerated)
                        resizedImages.Add(iconGenerated);
                    break;
                case "ios":
                    var appleAssetGen = new AppleIconAssetsGenerator(image, appIconName, IntermediateOutputPath, appIconDpis, this, generateImages: false);
                    var assetsGenerated = appleAssetGen.Generate();
                    foreach (var assetGenerated in assetsGenerated)
                        resizedImages.Add(assetGenerated);
                    break;
                case "uwp":
                    var windowsIconGen = new WindowsIconGenerator(image, IntermediateOutputPath, this, generateImages: false);
                    resizedImages.Add(windowsIconGen.Generate());
                    break;
            }
            foreach (var dpi in appIconDpis)
			{
				LogDebugMessage($"App Icon: " + dpi);

				var destination = Resizer.GetRasterFileDestination(image, dpi, IntermediateOutputPath)
					.Replace("{name}", appIconName);
				
				LogDebugMessage($"App Icon Destination: " + destination);
				resizedImages.Add(new ResizedImageInfo { Dpi = dpi, Filename = destination });
			}
        }

        IEnumerable<ResizeImageInfo> RemoveDuplicates(IEnumerable<ResizeImageInfo> inputImages)
        {
            var imagesPairs = new Dictionary<string, ResizeImageInfo>();

            foreach (var image in inputImages)
            {
                if (imagesPairs.ContainsKey(image.OutputName))
                {
                    continue;
                }
                imagesPairs[image.OutputName] = image;
            }

            return imagesPairs.Values;
        }

        void ILogger.Log(string message)
        {
            Log?.LogMessage(message);
        }
    }
}