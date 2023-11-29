namespace MovieSharp.Composers.Videos;

public interface ITransformedVideoClip
{
    ITransformedVideoClip AddTranslate(float x, float y);
    ITransformedVideoClip AddScale(float factor);
    ITransformedVideoClip AddScale(float x, float y);
    IVideoClip ToClip();
}
