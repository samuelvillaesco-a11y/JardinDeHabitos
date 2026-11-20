namespace JardinDeHabitos.Core;

/// <summary>Cómo se sintió el usuario. Se elige de una lista (varias a la vez).</summary>
public enum Emocion
{
    Alegria, Calma, Esperanza, Orgullo, Gratitud,
    Ansiedad, Tristeza, Irritabilidad, Enojo, Estres, Soledad, Aburrimiento, Culpa, Verguenza, Cansancio,
}

/// <summary>
/// Situaciones de riesgo de recaída (Marlatt y Gordon): estados emocionales negativos, conflicto con
/// otros y presión social explican la mayoría de las recaídas; más señales del entorno y del cuerpo.
/// </summary>
public enum Detonante
{
    Conflicto, PresionSocial, Celebracion, Soledad, Estres, Aburrimiento, Hambre, Cansancio, LugarOPersona, Dinero,
}

/// <summary>Síntomas que se califican de 0 (nada) a 4 (muy fuerte), como en la escala de Minnesota.</summary>
public enum Sintoma
{
    // Generales (abstinencia de casi todo: MNWS, criterios DSM-5).
    Ansiedad, Irritabilidad, AnimoBajo, Concentracion, Inquietud, MalDormir, Apetito, Cansancio,
    // Físicos (CIWA-Ar, COWS, abstinencia de cafeína).
    DolorCabeza, Temblor, Sudor, Nausea, DolorCuerpo, NarizLagrimeo, Taquicardia,
    // Señales de alarma: piden ayuda en el momento.
    Alucinaciones, Confusion, Convulsiones, Autolesion,
}

/// <summary>Cuándo aparece un síntoma en el registro y si es una señal de alarma.</summary>
public sealed record SintomaInfo(Sintoma Sintoma, string Nombre, MalHabito[]? SoloPara, bool Alarma = false)
{
    public bool Aplica(IReadOnlySet<MalHabito> plantados) =>
        SoloPara is null || SoloPara.Any(plantados.Contains);
}

/// <summary>
/// Una entrada del blog botánico: un día. Ánimo de 1 a 5 y ganas de 0 a 4 (ítem de "craving" de la
/// escala de Minnesota); los síntomas de 0 a 4. Todo es opcional salvo el día.
/// </summary>
public sealed record EntradaDiario(
    DateOnly Dia,
    int? Animo = null,
    int? Ganas = null,
    IReadOnlySet<Emocion>? Emociones = null,
    IReadOnlySet<Detonante>? Detonantes = null,
    IReadOnlyDictionary<Sintoma, int>? Sintomas = null,
    string? Nota = null)
{
    public const int LargoMaximoNota = 4000;

    public IReadOnlySet<Emocion> EmocionesO => Emociones ?? new HashSet<Emocion>();
    public IReadOnlySet<Detonante> DetonantesO => Detonantes ?? new HashSet<Detonante>();
    public IReadOnlyDictionary<Sintoma, int> SintomasO => Sintomas ?? new Dictionary<Sintoma, int>();

    public bool Vacia => Animo is null && Ganas is null && EmocionesO.Count == 0 && DetonantesO.Count == 0
                         && !SintomasO.Values.Any(v => v > 0) && string.IsNullOrWhiteSpace(Nota);

    public string? Problema()
    {
        if (Animo is { } a && a is < 1 or > 5) return "El ánimo va de 1 a 5.";
        if (Ganas is { } g && g is < 0 or > 4) return "Las ganas van de 0 a 4.";
        if (SintomasO.Values.Any(v => v is < 0 or > 4)) return "Cada síntoma va de 0 a 4.";
        if (Nota?.Length > LargoMaximoNota) return $"La nota puede tener hasta {LargoMaximoNota} caracteres.";
        if (Vacia) return "Escribe algo o marca cómo te sentiste.";
        return null;
    }
}

/// <summary>Qué hacer con lo que se registró: nada, hablar con un médico hoy, o pedir ayuda ya.</summary>
public enum Alerta { Ninguna, MedicoHoy, Urgente, Crisis }

public static class CatalogoDiario
{
    private static readonly MalHabito[] Abstinencia = [MalHabito.Alcohol, MalHabito.Sustancias];

    public static IReadOnlyList<(Emocion, string)> Emociones { get; } =
    [
        (Emocion.Alegria, "😊 Alegría"), (Emocion.Calma, "😌 Calma"), (Emocion.Esperanza, "🌱 Esperanza"),
        (Emocion.Orgullo, "💪 Orgullo"), (Emocion.Gratitud, "🙏 Gratitud"),
        (Emocion.Ansiedad, "😰 Ansiedad"), (Emocion.Tristeza, "😢 Tristeza"), (Emocion.Irritabilidad, "😤 Irritabilidad"),
        (Emocion.Enojo, "😠 Enojo"), (Emocion.Estres, "🌀 Estrés"), (Emocion.Soledad, "😶 Soledad"),
        (Emocion.Aburrimiento, "🥱 Aburrimiento"), (Emocion.Culpa, "😔 Culpa"), (Emocion.Verguenza, "🙈 Vergüenza"),
        (Emocion.Cansancio, "😴 Cansancio"),
    ];

    public static IReadOnlyList<(Detonante, string)> Detonantes { get; } =
    [
        (Detonante.Conflicto, "Discusión o conflicto"), (Detonante.PresionSocial, "Me ofrecieron o invitaron"),
        (Detonante.Celebracion, "Fiesta o celebración"), (Detonante.Soledad, "Estar a solas"),
        (Detonante.Estres, "Presión de trabajo o escuela"), (Detonante.Aburrimiento, "Aburrimiento, tiempo libre"),
        (Detonante.Hambre, "Hambre"), (Detonante.Cansancio, "Cansancio, poco sueño"),
        (Detonante.LugarOPersona, "Un lugar o persona que me lo recordó"), (Detonante.Dinero, "Dinero (cobro, deudas)"),
    ];

    public static IReadOnlyList<SintomaInfo> Sintomas { get; } =
    [
        new(Sintoma.Ansiedad, "Ansiedad o nervios", null),
        new(Sintoma.Irritabilidad, "Irritabilidad", null),
        new(Sintoma.AnimoBajo, "Ánimo bajo", null),
        new(Sintoma.Concentracion, "Me cuesta concentrarme", null),
        new(Sintoma.Inquietud, "Inquietud", null),
        new(Sintoma.MalDormir, "Dormí mal", null),
        new(Sintoma.Apetito, "Cambios en el apetito", null),
        new(Sintoma.Cansancio, "Cansancio o somnolencia", null),
        new(Sintoma.DolorCabeza, "Dolor de cabeza", [MalHabito.Alcohol, MalHabito.Cafeina, MalHabito.Sustancias]),
        new(Sintoma.Temblor, "Temblor", Abstinencia),
        new(Sintoma.Sudor, "Sudoración", Abstinencia),
        new(Sintoma.Nausea, "Náusea o malestar de estómago", [MalHabito.Alcohol, MalHabito.Cafeina, MalHabito.Sustancias]),
        new(Sintoma.DolorCuerpo, "Dolor de músculos o huesos", [MalHabito.Cafeina, MalHabito.Sustancias]),
        new(Sintoma.NarizLagrimeo, "Nariz que escurre, lagrimeo o bostezos", [MalHabito.Sustancias]),
        new(Sintoma.Taquicardia, "Corazón acelerado", Abstinencia),
        new(Sintoma.Alucinaciones, "Ver, oír o sentir cosas que no están", Abstinencia, Alarma: true),
        new(Sintoma.Confusion, "Confusión o no saber dónde estoy", Abstinencia, Alarma: true),
        new(Sintoma.Convulsiones, "Convulsiones o desmayos", Abstinencia, Alarma: true),
        new(Sintoma.Autolesion, "Pensamientos de hacerme daño", null, Alarma: true),
    ];

    public static SintomaInfo De(Sintoma s) => Sintomas.Single(i => i.Sintoma == s);

    /// <summary>Los síntomas que se preguntan según los árboles plantados.</summary>
    public static IEnumerable<SintomaInfo> Para(IReadOnlySet<MalHabito> plantados) => Sintomas.Where(s => s.Aplica(plantados));

    public static readonly string[] NombresIntensidad = ["Nada", "Leve", "Moderado", "Fuerte", "Muy fuerte"];
    public static readonly string[] NombresGanas = ["Ninguna", "Pocas", "Moderadas", "Fuertes", "Muy fuertes"];
    public static readonly string[] CarasAnimo = ["😞", "🙁", "😐", "🙂", "😄"];
    public static readonly string[] NombresAnimo = ["Muy mal", "Mal", "Más o menos", "Bien", "Muy bien"];

    /// <summary>
    /// Señales que no esperan al día siguiente. Pensar en hacerse daño → crisis (Línea de la Vida / 911).
    /// Alucinaciones, confusión o convulsiones en una abstinencia de alcohol o sustancias → urgencia (911):
    /// pueden ser delirium tremens o convulsiones por abstinencia. Temblor o sudor fuertes (CIWA-Ar) → médico hoy.
    /// </summary>
    public static Alerta Evaluar(EntradaDiario e)
    {
        var s = e.SintomasO;
        int V(Sintoma x) => s.GetValueOrDefault(x);
        if (V(Sintoma.Autolesion) > 0) return Alerta.Crisis;
        if (V(Sintoma.Alucinaciones) > 0 || V(Sintoma.Confusion) > 0 || V(Sintoma.Convulsiones) > 0) return Alerta.Urgente;
        if (V(Sintoma.Temblor) >= 3 || V(Sintoma.Sudor) >= 3 || V(Sintoma.Taquicardia) >= 3) return Alerta.MedicoHoy;
        return Alerta.Ninguna;
    }

    /// <summary>
    /// Lo que suele pasar con el tiempo, por hábito plantado. Tiempos de fuentes médicas (ver
    /// docs/superpowers/specs/2026-11-20-blog-botanico-design.md).
    /// </summary>
    public static string? Fundamento(MalHabito h) => h switch
    {
        MalHabito.Alcohol =>
            "La abstinencia aguda empieza entre 6 y 24 h después de la última copa, es más fuerte entre las 24 y 72 h y suele pasar en 5 a 7 días. " +
            "Ver u oír cosas, confusión o convulsiones son una urgencia.",
        MalHabito.Cigarro =>
            "Irritabilidad, ansiedad, mal sueño y más apetito son más fuertes la primera semana y bajan en 2 a 4 semanas. " +
            "Las ganas duran más, pero cada vez llegan menos seguido y pasan en minutos.",
        MalHabito.Cafeina =>
            "El dolor de cabeza y el cansancio empiezan entre 12 y 24 h, son más fuertes entre las 20 y 51 h y duran de 2 a 9 días.",
        MalHabito.Sustancias =>
            "Con opioides de acción corta, la abstinencia empieza entre 8 y 24 h y dura de 4 a 10 días; con otras sustancias varía mucho. " +
            "Dejar de golpe tranquilizantes u opioides puede ser peligroso: busca acompañamiento médico.",
        MalHabito.Apuestas =>
            "Al intentar dejarlo es común sentir inquietud o irritabilidad (DSM-5). Anotar qué lo detona ayuda a anticiparte.",
        MalHabito.Pornografia or MalHabito.Pantallas or MalHabito.Compras =>
            "No hay una abstinencia física, pero las ganas aparecen con detonantes: aburrimiento, soledad, estrés o la hora del día. Reconocerlos es la mitad del trabajo.",
        MalHabito.Azucares or MalHabito.Harinas or MalHabito.Sal =>
            "El gusto se ajusta: tras unas semanas comiendo menos, lo muy dulce o salado se siente más intenso. Un hábito nuevo tarda en promedio 66 días en volverse automático.",
        _ => null,
    };

    public const string FundamentoRegistro =
        "Anotar cada día cómo te sientes, qué tantas ganas tuviste y qué las detonó es parte central de la terapia cognitivo-conductual " +
        "y de la prevención de recaídas: ayuda a ver patrones y a preparar un plan para los momentos difíciles.";
}
