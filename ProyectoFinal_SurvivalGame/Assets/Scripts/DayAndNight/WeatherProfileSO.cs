using UnityEngine;

[CreateAssetMenu(fileName = "WeatherProfile", menuName = "Weather/Weather Profile")]
public class WeatherProfileSO : ScriptableObject
{
    [Header("Colores del DÍA")]
    public Gradient dayDirectionalColor;
    public Gradient dayAmbientColor;
    public Gradient dayFogColor;

    [Header("Cielo DÍA (Skybox)")]
    public Gradient daySkyTop;
    public Gradient daySkyBottom;
    public Gradient dayHorizon;

    [Header("Colores de la NOCHE")]
    public Gradient nightDirectionalColor;
    public Gradient nightAmbientColor;
    public Gradient nightFogColor;

    [Header("Cielo NOCHE (Skybox)")]
    public Gradient nightSkyTop;
    public Gradient nightSkyBottom;
    public Gradient nightHorizon;
}
