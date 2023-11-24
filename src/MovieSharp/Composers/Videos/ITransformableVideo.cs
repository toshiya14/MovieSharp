using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Composers.Videos;

public interface ITransformableVideo
{
    void AddTranslate(float x, float y);
    void AddScale(float factor);
    void AddScale(float x, float y);
}
