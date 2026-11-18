namespace JardinDeHabitos.Core;

public class StrikeState
{
    public int RachaConsecutivaDeDiasRotos { get; set; }
    public int StrikesTotalesDelArbolActual { get; set; }
    public PenaltyLevel NivelDePenalizacion { get; set; }
    public DateOnly? FechaFinDePenalizacionActiva { get; set; }
    public DateOnly? FechaUltimoCheckIn { get; set; }
}
