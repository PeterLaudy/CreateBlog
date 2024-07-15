using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Xml;
using SkiaSharp;

namespace CreateBlog
{
    /// <summary>
    /// Class representing the CSS of the div in which we embed the image.
    /// </summary>
    internal class ImageDivCss
    {
        /// <summary>
        /// Intialize a new instance of the <see cref="ImageDivCss"/> class.
        /// </summary>
        /// <param name="width">The width that this image will take as part of the collection.</param>
        /// <param name="totalWidth">The total width of all images in the collection.</param>
        /// <param name="imgQty">The number of images in the collection.</param>
        public ImageDivCss(int width, int totalWidth, int imgQty)
        {
            Class = $"scale{width}-{totalWidth}-{imgQty}";
            CSS = $$"""
            div.{{Class}} {
              width: calc({{width}}*(100% - {{(imgQty - 1) * 10}}px) / {{totalWidth}});
              object-fit: contain;
            }
            """;
        }

        /// <summary>
        /// Gets a value indicating the CSS for the div containing the image.
        /// </summary>
        public string CSS { get; }

        /// <summary>
        /// Gets a value indicating the class for the div containing the image.
        /// </summary>
        public string Class { get; }
    }

    /// <summary>
    /// A class representing the data about the images (not icons) in the HTML files.
    /// </summary>
    internal class ImageInfo
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="ImageInfo"/> class.
        /// </summary>
        /// <param name="fullFileName">The absolute path to the image file.</param>
        public ImageInfo(string fullFileName)
        {
            FileName = fullFileName;
            IsRelativeFileName = false;
            if ((null != Settings.Single.ImagesToCheckForMultipleUse) &&
                (Settings.Single.ImagesToCheckForMultipleUse!.Contains($";{Path.GetExtension(fullFileName)};")))
            {
                using var info = SKCodec.Create(FileName);
                Width = info.Info.Width;
                Height = info.Info.Height;
            }
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="ImageInfo"/> class.
        /// </summary>
        /// <param name="absoluteImageInfo">An instance of this same class with the absolute path to the image file.</param>
        /// <param name="htmlFileName">The HTML file name from which we need a relative path to the image file.</param>
        /// <param name="imgNode">The XML node to the image description in the source file.</param>
        /// <exception cref="ArgumentException">The <see cref="absoluteImageInfo"/> needs to have a path to the absolute image file.</exception>
        public ImageInfo(ImageInfo absoluteImageInfo, string htmlFileName, XmlNode? imgNode)
        {
            if (absoluteImageInfo.IsRelativeFileName)
            {
                throw new ArgumentException($"{nameof(ImageInfo)} needs to be absolute.");
            }

            var img = Path.Combine(Settings.Single.HtmlRootFolder!, absoluteImageInfo.FileName.Substring(Settings.Single.SourceRootFolder!.Length));

            FileName = Path.GetRelativePath(Path.GetDirectoryName(htmlFileName)!, img!).Replace('\\', '/');
            IsRelativeFileName = true;
            Width = absoluteImageInfo.Width;
            Height = absoluteImageInfo.Height;
            ImgNode = imgNode;
        }

        /// <summary>
        /// Set the CSS for the given parameters.
        /// </summary>
        /// <param name="width">The width that this image will take as part of the collection.</param>
        /// <param name="totalWidth">The total width of all images in the collection.</param>
        /// <param name="imgQty">The number of images in the collection.</param>
        public void SetCSS(int width, int totalWidth, int imgQty)
        {
            CssForDiv = new ImageDivCss(width, totalWidth, imgQty);
        }

        public string FileName { get; }
 
        public bool IsRelativeFileName { get; }
 
        public int Width { get; set; }
 
        public int Height { get; set; }

        public WND? WidthWND { get; set; }
 
        public XmlNode? ImgNode { get; private set; }

        public ImageDivCss? CssForDiv { get; private set; }
    }

    /// <summary>
    /// Class representing a list of <see cref="ImageInfo"/> items.
    /// </summary>
    internal class ImageInfoList : ObservableCollection<ImageInfo>
    {
        private bool cssIsSet = false;
        private List<int> dimList = new();
        private int totalWidth = 0;
        private int maxWidth = 0;
        private int minHeight = int.MaxValue;
        private int maxHeight = 0;

        /// <summary>
        /// Initialize a new instance of the <see cref="ImageInfoList"/> class.
        /// </summary>
        public ImageInfoList()
        {
            CollectionChanged += this.ItemAdded;
        }

        /// <summary>
        /// If an item is added to the collection, we already perform some calculations.
        /// </summary>
        /// <param name="sender">The object which created this event.</param>
        /// <param name="e">The detailed parameters for this event.</param>
        /// <exception cref="ReadOnlyException">We can only add images as long as we have not called SetCSS</exception>
        private void ItemAdded(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (null != e.NewItems)
            {
                if (cssIsSet)
                {
                    throw new ReadOnlyException("Items cannot be added if the CSS is calculated.");
                }

                foreach (ImageInfo imgInfo in e.NewItems!)
                {
                    dimList.Add(imgInfo.Width);
                    dimList.Add(imgInfo.Height);
                    maxWidth = Math.Max(maxWidth, imgInfo.Width);
                    minHeight = Math.Min(minHeight, imgInfo.Height);
                    maxHeight = Math.Max(maxHeight, imgInfo.Height);
                }
            }
        }

        /// <summary>
        /// Set the css for the div parent element of the image. 
        /// </summary>
        /// <remarks>
        /// It pays off is the images all have the same dimensions as much as possible.
        /// </remarks>
        public void SetCSS()
        {
            cssIsSet = true;

            var gcd = MathUtilities.GetGCD(dimList);

            maxWidth /= gcd;
            minHeight /= gcd;
            maxHeight /= gcd;

            var mul = 1;
            foreach (ImageInfo img in this)
            {
                img.Width /= gcd;
                img.Height /= gcd;
                img.WidthWND = new(img.Width * minHeight, img.Height);
                mul *= img.WidthWND.Divisor;
            }

            dimList.Clear();
            foreach (ImageInfo img in this)
            {
                totalWidth += mul * img.WidthWND!.Dividend / img.WidthWND!.Divisor;
                dimList.Add(mul * img.WidthWND!.Dividend / img.WidthWND!.Divisor);
            }

            gcd = MathUtilities.GetGCD(dimList);
            totalWidth /= gcd;

            foreach (ImageInfo img in this)
            {
                var w = mul * img.WidthWND!.Dividend / (gcd * img.WidthWND!.Divisor);
                img.SetCSS(w, totalWidth, Count);
            }
        }
    }
}