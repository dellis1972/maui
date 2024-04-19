using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Maui.Resizetizer
{
	internal class AppleIconAssetsGenerator
	{
		private JsonSerializerOptions options;
		private bool generateImages = true;

		public AppleIconAssetsGenerator(ResizeImageInfo info, string appIconName, string intermediateOutputPath, DpiPath[] dpis, ILogger logger, bool generateImages = true)
		{
			Info = info;
			Logger = logger;
			IntermediateOutputPath = intermediateOutputPath;
			AppIconName = appIconName;
			Dpis = dpis;
			options = new JsonSerializerOptions { WriteIndented = true };
			this.generateImages = generateImages;
		}

		public string AppIconName { get; }

		public DpiPath[] Dpis { get; }

		public ResizeImageInfo Info { get; private set; }
		public string IntermediateOutputPath { get; private set; }
		public ILogger Logger { get; private set; }

		public IEnumerable<ResizedImageInfo> Generate()
		{
			var outputAppIconSetDir = Path.Combine(IntermediateOutputPath, DpiPath.Ios.AppIconPath.Replace("{name}", AppIconName));
			
			Logger.Log("iOS App Icon Set Directory: " + outputAppIconSetDir);

			Directory.CreateDirectory(outputAppIconSetDir);

			var appIconSetContentsFile = Path.Combine(outputAppIconSetDir, "Contents.json");

			var (_, sourceModified) = Utils.FileExists(Info.Filename);
			var (_, destinationModified) = Utils.FileExists(appIconSetContentsFile);

			if (destinationModified > sourceModified)
			{
				Logger.Log($"Skipping `{Info.Filename}` => `{appIconSetContentsFile}` file is up to date.");
				return new List<ResizedImageInfo> {
					new ResizedImageInfo { Dpi = new DpiPath("", 1), Filename = appIconSetContentsFile }
				};
			}

			var appIconImagesJson = new JsonArray();

			foreach (var dpi in Dpis)
			{
				foreach (var idiom in dpi.Idioms)
				{
					var w = dpi.Size.Value.Width.ToString("0.#", CultureInfo.InvariantCulture);
					var h = dpi.Size.Value.Height.ToString("0.#", CultureInfo.InvariantCulture);
					var s = dpi.Scale.ToString("0", CultureInfo.InvariantCulture);

					var imageIcon = new JsonObject
					{
						["idiom"] = idiom,
						["size"] = $"{w}x{h}",
						["scale"] = $"{s}x",
						["filename"] = AppIconName + dpi.FileSuffix + Resizer.RasterFileExtension,
					};

					appIconImagesJson.Add(imageIcon);
				}
			}

			var appIconContentsJson = new JsonObject
			{
				["images"] = appIconImagesJson,
				["properties"] = new JsonObject(),
				["info"] = new JsonObject
				{
					["version"] = 1,
					["author"] = "xcode",
				},
			};

			if (generateImages)
				File.WriteAllText(appIconSetContentsFile, appIconContentsJson.ToJsonString(options));

			return new List<ResizedImageInfo> {
				new ResizedImageInfo { Dpi = new DpiPath("", 1), Filename = appIconSetContentsFile }
			};
		}
	}
}
