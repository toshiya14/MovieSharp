using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers.Videos;

public interface ITransformedVideoClip
{
    ITransformedVideoClip AddTranslate(float x, float y);
    ITransformedVideoClip AddScale(float factor);
    ITransformedVideoClip AddScale(float x, float y);
    IVideoClip ToClip();
}
