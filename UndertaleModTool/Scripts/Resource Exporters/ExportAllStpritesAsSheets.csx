using System;
using System.IO;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;
using UndertaleModLib.Scripting;
using ImageMagick;
using ImageMagick.Drawing;
using UndertaleModLib.Models;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

string Output = Path.Combine(Path.GetDirectoryName(FilePath), "sprite_sheets");
Directory.CreateDirectory(Output);

SetProgressBar(null, "Exporting sprites", 0, Data.Sprites.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await CreateSheets();
}

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete!");

async Task CreateSheets()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites.Where(s => s != null), CreateSheet));
}

void CreateSheet(UndertaleSprite sprite)
{
    int w = (int)sprite.Width, h = (int)sprite.Height, n = sprite.Textures.Count;
    int space = 2;

    if (n == 0) { IncrementProgressParallel(); return; }

    var best = Enumerable.Range(1, n).Select(cols =>
    {
        int rows = (n + cols - 1) / cols;
        int width = cols * w + (cols - 1) * space;
        int height = rows * h + (rows - 1) * space;
        int area = width * height;
        double ratio = (double)Math.Max(width, height) / Math.Min(width, height);
        return new { cols, rows, width, height, area, ratio };
    })
    .OrderBy(x => x.ratio)
    .ThenBy(x => x.area)
    .First();

    MagickImage atlas = new(new MagickColor(MagickColors.Transparent), (uint)best.width, (uint)best.height);
    for (int i = 0; i < n; i++)
    {
        if (sprite.Textures[i].Texture is null) continue;
        using (var src = worker.GetTextureFor(sprite.Textures[i].Texture, sprite.Name.Content, true))
        {
            atlas.Composite(src, ((i % best.cols) * (w + space)), ((i / best.cols) * (h + space)), CompositeOperator.Over);
        }
    }
    atlas.Write(Path.Combine(Output, $"{sprite.Name.Content}.png"), MagickFormat.Png32);
    atlas.Dispose();
    IncrementProgressParallel();
}