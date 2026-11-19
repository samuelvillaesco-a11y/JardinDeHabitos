using System.Globalization;

namespace JardinDeHabitos.Core;

/// <summary>Los malos hábitos que se pueden elegir. El usuario nunca escribe un nombre.</summary>
public enum MalHabito
{
    Alcohol,
    Apuestas,
    Azucares,
    Cafeina,
    Cigarro,
    Compras,
    Harinas,
    Pornografia,
    Pantallas,
    Sal,
    Sustancias,
    Otros,
}

/// <summary>Qué modos admite cada hábito y si necesita aviso médico al elegirlo.</summary>
public sealed record HabitoInfo(MalHabito Habito, string Nombre, bool PermiteModerar, bool PermiteAbstinencia, string? AvisoMedico)
{
    public IEnumerable<HabitType> Modos =>
        new[] { (PermiteAbstinencia, HabitType.AbstinenciaTotal), (PermiteModerar, HabitType.Moderable) }
            .Where(m => m.Item1).Select(m => m.Item2);
}

public static class CatalogoHabitos
{
    private const string Linea = "Línea de la Vida (gratis, 24 h): 800 911 2000. Urgencias: 911.";

    private const string AvisoAlcohol =
        "Si bebes a diario o tienes temblores, sudor o ansiedad cuando no bebes, habla con un médico antes de dejarlo: " +
        "dejar el alcohol de golpe puede causar convulsiones. " + Linea;

    private const string AvisoSustancias =
        "Algunas sustancias (como tranquilizantes u opioides) no se deben dejar de golpe: pueden causar una abstinencia peligrosa. " +
        "Busca acompañamiento médico para dejarlas con seguridad. " + Linea;

    private static readonly CultureInfo Cultura = new("es-MX");

    private static readonly HabitoInfo[] Datos =
    [
        new(MalHabito.Alcohol, "Alcohol", PermiteModerar: true, PermiteAbstinencia: true, AvisoAlcohol),
        new(MalHabito.Apuestas, "Apuestas", PermiteModerar: false, PermiteAbstinencia: true, null),
        new(MalHabito.Azucares, "Azúcares", true, true, null),
        new(MalHabito.Cafeina, "Cafeína", true, true, null),
        new(MalHabito.Cigarro, "Cigarro y vapeo", true, true, null),
        new(MalHabito.Compras, "Compras impulsivas", true, true, null),
        new(MalHabito.Harinas, "Harinas procesadas", true, true, null),
        new(MalHabito.Pornografia, "Pornografía", false, true, null),
        new(MalHabito.Pantallas, "Redes sociales y pantallas", true, true, null),
        new(MalHabito.Sal, "Sal", true, true, null),
        new(MalHabito.Sustancias, "Sustancias", false, true, AvisoSustancias),
        new(MalHabito.Otros, "Otros", true, true, null),
    ];

    /// <summary>Orden alfabético; "Otros" siempre al final.</summary>
    public static IReadOnlyList<HabitoInfo> Todos { get; } =
    [
        .. Datos.Where(d => d.Habito != MalHabito.Otros).OrderBy(d => d.Nombre, StringComparer.Create(Cultura, ignoreCase: true)),
        Datos.Single(d => d.Habito == MalHabito.Otros),
    ];

    public static HabitoInfo De(MalHabito habito) => Datos.Single(d => d.Habito == habito);

    /// <summary>Para migrar árboles viejos que tenían nombre libre.</summary>
    public static MalHabito DesdeNombreLibre(string nombre)
    {
        var n = SinAcentos(nombre.Trim().ToLowerInvariant());
        (string Clave, MalHabito Habito)[] claves =
        [
            ("alcohol", MalHabito.Alcohol), ("cerveza", MalHabito.Alcohol), ("beber", MalHabito.Alcohol),
            ("apuesta", MalHabito.Apuestas), ("casino", MalHabito.Apuestas),
            ("azucar", MalHabito.Azucares), ("dulce", MalHabito.Azucares), ("refresco", MalHabito.Azucares),
            ("cafe", MalHabito.Cafeina),
            ("cigarr", MalHabito.Cigarro), ("fumar", MalHabito.Cigarro), ("vape", MalHabito.Cigarro), ("tabaco", MalHabito.Cigarro),
            ("compra", MalHabito.Compras),
            ("harina", MalHabito.Harinas), ("pan", MalHabito.Harinas),
            ("porno", MalHabito.Pornografia),
            ("redes", MalHabito.Pantallas), ("pantalla", MalHabito.Pantallas), ("celular", MalHabito.Pantallas),
            ("sal", MalHabito.Sal),
            ("droga", MalHabito.Sustancias), ("sustancia", MalHabito.Sustancias), ("marihuana", MalHabito.Sustancias),
        ];
        foreach (var (clave, habito) in claves)
        {
            bool coincide = clave.Length <= 3 ? n == clave || n.Split(' ').Contains(clave) : n.Contains(clave);
            if (coincide) return habito;
        }
        return MalHabito.Otros;
    }

    private static string SinAcentos(string s) =>
        new([.. s.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)]);
}
