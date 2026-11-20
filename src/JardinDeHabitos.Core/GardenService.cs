namespace JardinDeHabitos.Core;

/// <summary>
/// Lo que la UI necesita de un árbol, ya calculado para "hoy". No trae el nombre del hábito:
/// fuera del estante solo se muestra la especie.
/// </summary>
public sealed record VistaArbol(
    Guid Id,
    MalHabito Categoria,
    string Especie,
    HabitType Tipo,
    GrowthStage Etapa,
    int Dias,
    bool Plaga,
    bool Raices,
    DateOnly? FinCuidado,
    int Strikes,
    int StrikesAbandono,
    bool PideCheckInHoy,
    bool EsCheatDayHoy,
    bool RegadoHoy,
    bool PodadoHoy,
    bool TratadoHoy,
    bool PuedeBorron,
    int DiasSinVisita,
    MiniEvento MiniEvento,
    bool PideAsistencia,
    bool AsistioHoy,
    IReadOnlyList<DayOfWeek> DiasDeCheat)
{
    public PenaltyLevel Penalizacion => Raices ? PenaltyLevel.Strike2 : Plaga ? PenaltyLevel.Strike1 : PenaltyLevel.Ninguna;
    public bool EnCuidado => Plaga || Raices;
}

public sealed record ResultadoCheckIn(CheckInOutcome Outcome);

public enum MiniJuegoTipo
{
    Regar,
    Podar,
    Plaga,
    Raices,
}

public enum TipoAviso
{
    Etapa,
    MiniEvento,
    PlagaPorAbandono,
    MuertePorAbandono,
}

/// <summary>Algo que contar en una notificación. Solo trae la especie, nunca el hábito.</summary>
public sealed record Aviso(TipoAviso Tipo, string Especie, GrowthStage Etapa = GrowthStage.Semilla);

/// <summary>Punto de entrada de la app: orquesta reglas, estadísticas, logros y persistencia.</summary>
public sealed class GardenService(IGardenStore store, GrowthStageCalculator calculadora)
{
    public const int MaxHabitos = 10;
    public const int MaxDiasPrevios = 365 * 50;

    private const string SinAnunciar = "sinAnunciar";
    private const string UltimoDiaCheckIn = "ultimoDiaCheckIn";
    private const string SeguidosActual = "seguidosActual";
    private const string UltimoDiaRiego = "ultimoDiaRiego";
    private const string RachaRiegoActual = "rachaRiegoActual";
    private const string UltimoDiaPoda = "ultimoDiaPoda";
    private const string UltimoDiaNocturno = "ultimoDiaNocturno";
    private const string UltimoDiaMadrugada = "ultimoDiaMadrugada";
    private const string UltimoDiaMimo = "ultimoDiaMimo";
    private const string UltimoDiaJardinCompleto = "ultimoDiaJardinCompleto";
    private const string CenizaHabito = "cenizaHabito";
    private const string Visto = "visto:";

    private static readonly GrowthStage[] EtapasAvisables =
        [GrowthStage.Flor, GrowthStage.FrutaInmadura, GrowthStage.FrutaComestible, GrowthStage.FrutaLegendaria];

    private readonly CheckInProcessor _procesador = new(calculadora);

    public DateOnly Hoy => calculadora.Hoy;

    // ---------- Lectura ----------

    public IReadOnlyList<VistaArbol> Jardin()
    {
        Sincronizar();
        return [.. store.Cargar().Select(Vista)];
    }

    /// <summary>Un árbol de cerca. Abrirlo limpia sus avisos pendientes (ya los vio).</summary>
    public VistaArbol Arbol(Guid id)
    {
        Sincronizar();
        var r = Buscar(id);
        Cambiar(s =>
        {
            foreach (var k in new[] { "avisoEvento", "avisoPlaga", "avisoMuerte" }) s.Set($"{k}:{id}", 0);
        });
        return Vista(r);
    }

    public IReadOnlyList<string> EspeciesLibres()
    {
        var usadas = store.Cargar().Select(r => r.Habito.EspecieDeArbol).ToHashSet();
        return [.. EspeciesDeArbol.Todas.Where(e => !usadas.Contains(e))];
    }

    /// <summary>Hábitos que aún se pueden plantar (uno por hábito, salvo "Otros"), en orden del catálogo.</summary>
    public IReadOnlyList<HabitoInfo> HabitosDisponibles()
    {
        var plantados = store.Cargar().Select(r => r.Habito.Categoria).ToHashSet();
        return [.. CatalogoHabitos.Todos.Where(h => h.Habito == MalHabito.Otros || !plantados.Contains(h.Habito))];
    }

    /// <summary>El estante de trofeos: solo lo que el usuario puede ver hoy.</summary>
    public IReadOnlyList<EntradaEstante> Estante()
    {
        Sincronizar();
        var stats = store.Estadisticas();
        var tenidos = store.NivelesLogro();
        var arboles = store.Cargar();
        return
        [
            .. CatalogoLogros.Visibles(stats, tenidos).Select(def =>
            {
                var mios = tenidos.Where(t => t.Id == def.Id).ToList();
                var top = mios.MaxBy(t => t.Nivel);
                string? arbol = def.Habito is { } h
                    ? arboles.FirstOrDefault(r => r.Habito.Categoria == h)?.Habito.EspecieDeArbol
                    : null;
                int nivel = top?.Nivel ?? 0;
                bool nuevo = nivel > stats.GetValueOrDefault(Visto + def.Id);
                return new EntradaEstante(def, nivel, top?.Fecha, stats.GetValueOrDefault(def.Clave), arbol, nuevo);
            }),
        ];
    }

    /// <summary>El usuario abrió el emblema: deja de marcarse como nuevo (hasta su siguiente estrella).</summary>
    public void MarcarVisto(string logroId)
    {
        int nivel = store.NivelesLogro().Where(n => n.Id == logroId).Select(n => n.Nivel).DefaultIfEmpty(0).Max();
        Cambiar(s => s.Max(Visto + logroId, nivel));
    }

    public IReadOnlySet<Debuff> Debuffs() => EvaluadorLogros.Debuffs(store.NivelesLogro());

    /// <summary>Cuántos logros se ganaron desde la última vez que se anunciaron. Los marca como anunciados.</summary>
    public int TomarLogrosNuevos()
    {
        Sincronizar();
        int n = store.Estadisticas().GetValueOrDefault(SinAnunciar);
        if (n > 0) Cambiar(s => s.Set(SinAnunciar, 0));
        return n;
    }

    /// <summary>Avisos para notificaciones (etapas, emergencias, plagas por abandono). Los marca como dados.</summary>
    public IReadOnlyList<Aviso> TomarAvisos()
    {
        Sincronizar();
        var avisos = new List<Aviso>();
        Cambiar(s =>
        {
            foreach (var r in store.Cargar())
            {
                var id = r.Habito.Id;
                var especie = r.Habito.EspecieDeArbol;
                var etapa = calculadora.CalcularEtapa(r.Habito);
                if ((int)etapa > s.Get($"avisoEtapa:{id}") && EtapasAvisables.Contains(etapa))
                    avisos.Add(new Aviso(TipoAviso.Etapa, especie, etapa));
                s.Set($"avisoEtapa:{id}", Math.Max(s.Get($"avisoEtapa:{id}"), (int)etapa));

                if (s.Get($"avisoMuerte:{id}") > 0) avisos.Add(new Aviso(TipoAviso.MuertePorAbandono, especie));
                else if (s.Get($"avisoPlaga:{id}") > 0) avisos.Add(new Aviso(TipoAviso.PlagaPorAbandono, especie));
                else if (s.Get($"avisoEvento:{id}") > 0) avisos.Add(new Aviso(TipoAviso.MiniEvento, especie));
                foreach (var k in new[] { "avisoEvento", "avisoPlaga", "avisoMuerte" }) s.Set($"{k}:{id}", 0);
            }
        });
        return avisos;
    }

    /// <summary>Corre el avance diario y evalúa logros. Devuelve cuántos logros nuevos hay sin anunciar.</summary>
    public int Refrescar()
    {
        Sincronizar();
        return store.Estadisticas().GetValueOrDefault(SinAnunciar);
    }

    // ---------- Acciones ----------

    public VistaArbol CrearHabito(MalHabito habito, string especie, HabitType tipo, IEnumerable<DayOfWeek> diasDeCheat,
        int diasPrevios = 0, bool grupo = false, bool terapia = false)
    {
        Sincronizar();
        var existentes = store.Cargar();
        if (existentes.Count >= MaxHabitos)
            throw new InvalidOperationException($"Tu jardín ya tiene {MaxHabitos} árboles.");
        if (!EspeciesDeArbol.Todas.Contains(especie))
            throw new ArgumentException("Esa especie no existe.");
        if (existentes.Any(r => r.Habito.EspecieDeArbol == especie))
            throw new InvalidOperationException("Ese árbol ya está en tu jardín. Elige otro.");
        if (habito != MalHabito.Otros && existentes.Any(r => r.Habito.Categoria == habito))
            throw new InvalidOperationException("Ya tienes un árbol para ese hábito.");
        if (!CatalogoHabitos.De(habito).Modos.Contains(tipo))
            throw new InvalidOperationException("Ese hábito solo se puede dejar por completo.");
        if (diasPrevios is < 0 or > MaxDiasPrevios)
            throw new ArgumentException("El tiempo previo no es válido.");

        var h = new Habit
        {
            Categoria = habito,
            EspecieDeArbol = especie,
            Tipo = tipo,
            FechaDeInicioRachaActual = Hoy,
            DiasDeCrecimiento = diasPrevios,
            ApoyoGrupo = grupo,
            ApoyoTerapia = terapia,
            DiasDeCheat = [.. diasDeCheat],
        };
        CheatDaySchedule.Validar(h);

        var r = new HabitRecord
        {
            Habito = h,
            Estado = new StrikeState { UltimoDiaEvaluado = Hoy.AddDays(-1) },
            Orden = existentes.Count == 0 ? 0 : existentes.Max(x => x.Orden) + 1,
        };
        store.EnTransaccion(() =>
        {
            store.Guardar(r);
            Cambiar(s =>
            {
                s.Set(Stat.De(Stat.Elegido, habito), 1);
                if (tipo == HabitType.Moderable) s.Set(Stat.De(Stat.Moderado, habito), 1);
                s.Inc(Stat.Plantados);
                // El tiempo previo no se anuncia como etapa nueva.
                s.Set($"avisoEtapa:{h.Id}", (int)calculadora.CalcularEtapa(h));
                ActualizarRachas(s, r);
            });
            EvaluarLogros();
        });
        return Vista(r);
    }

    public ResultadoCheckIn CheckIn(Guid id, CheckInResult resultado)
    {
        Sincronizar();
        var r = Buscar(id);
        var e = r.Estado;
        int faltasPrevias = e.DiasNormalesSinCheckIn;
        var ultimoPrevio = e.FechaUltimoCheckIn;
        var cumplidoPrevio = r.UltimoCumplido;

        var outcome = _procesador.Procesar(r.Habito, e, resultado);
        var hoy = Hoy;
        var cat = r.Habito.Categoria;

        store.EnTransaccion(() =>
        {
            Cambiar(s =>
            {
                // Constancia: días seguidos sin faltar (los días libres no rompen).
                r.ConstanciaActual = ultimoPrevio is not null && faltasPrevias == 0 ? r.ConstanciaActual + 1 : 1;
                s.Max(Stat.De(Stat.Constancia, cat), r.ConstanciaActual);

                int ultimoDia = s.Get(UltimoDiaCheckIn);
                if (ultimoDia != hoy.DayNumber)
                {
                    s.Inc(Stat.DiasConCheckIn);
                    s.Set(SeguidosActual, ultimoDia == hoy.DayNumber - 1 ? s.Get(SeguidosActual) + 1 : 1);
                    s.Max(Stat.SeguidosConCheckIn, s.Get(SeguidosActual));
                    s.Set(UltimoDiaCheckIn, hoy.DayNumber);
                }

                if (resultado != CheckInResult.CheatDay) s.Inc(Stat.De(Stat.CheckIns, cat));

                if (resultado == CheckInResult.Cumplido)
                {
                    r.UltimoCumplido = hoy;
                    if (hoy.DayOfWeek == DayOfWeek.Sunday)
                    {
                        var sabado = hoy.AddDays(-1);
                        bool sabadoBien = cumplidoPrevio == sabado
                            || (CheatDaySchedule.EsDiaDeCheat(r.Habito, sabado) && faltasPrevias == 0);
                        if (sabadoBien) s.Inc(Stat.De(Stat.FinesDeSemana, cat));
                    }
                }

                if (resultado == CheckInResult.Roto)
                {
                    s.Inc(Stat.Honestos);
                    if (outcome.ArbolMurio)
                    {
                        RegistrarMuerte(s, r, outcome.DiasAntesDeMorir, abandono: false);
                    }
                    else if (outcome.EmpezoCuidado && !r.CuidadoPendiente)
                    {
                        r.CuidadoPendiente = true;
                        r.TratamientosEnCuidado = 0;
                    }
                }
                ActualizarRachas(s, r);
            });
            store.Guardar(r);
            EvaluarLogros();
        });
        return new ResultadoCheckIn(outcome);
    }

    public void RegistrarAsistencia(Guid id) => Modificar(id, (r, s) =>
    {
        if (!r.Habito.ApoyoGrupo && !r.Habito.ApoyoTerapia)
            throw new InvalidOperationException("Este árbol no tiene grupo ni terapia registrados.");
        if (r.UltimaAsistencia == Hoy)
            throw new InvalidOperationException("La asistencia de hoy ya quedó registrada.");
        r.UltimaAsistencia = Hoy;
        s.Inc(Stat.De(Stat.Asistencias, r.Habito.Categoria));
        s.Inc(Stat.AsistenciasTotales);
    });

    /// <summary>Un mini-juego terminado. <paramref name="piezas"/> = ramas, bichos o raíces quitados.</summary>
    public void RegistrarJuego(Guid id, MiniJuegoTipo juego, int piezas = 0) => Modificar(id, (r, s) =>
    {
        var hoy = Hoy;
        int d = hoy.DayNumber;
        switch (juego)
        {
            case MiniJuegoTipo.Regar:
                if (r.UltimoRiego == hoy) return;
                r.UltimoRiego = hoy;
                if (s.Get(UltimoDiaRiego) != d)
                {
                    s.Inc(Stat.DiasRegados);
                    s.Set(RachaRiegoActual, s.Get(UltimoDiaRiego) == d - 1 ? s.Get(RachaRiegoActual) + 1 : 1);
                    s.Max(Stat.RachaRiego, s.Get(RachaRiegoActual));
                    s.Set(UltimoDiaRiego, d);
                }
                int hora = calculadora.AhoraLocal.Hour;
                if (hora >= 21 && s.Get(UltimoDiaNocturno) != d) { s.Inc(Stat.RiegosNocturnos); s.Set(UltimoDiaNocturno, d); }
                if (hora < 7 && s.Get(UltimoDiaMadrugada) != d) { s.Inc(Stat.RiegosMadrugada); s.Set(UltimoDiaMadrugada, d); }
                var todos = store.Cargar();
                if (todos.Count >= 2 && todos.All(x => x.Habito.Id == id || x.UltimoRiego == hoy) && s.Get(UltimoDiaJardinCompleto) != d)
                {
                    s.Inc(Stat.JardinCompleto);
                    s.Set(UltimoDiaJardinCompleto, d);
                }
                break;
            case MiniJuegoTipo.Podar:
                if (r.UltimaPoda == hoy) return;
                r.UltimaPoda = hoy;
                s.Inc(Stat.Ramas, Math.Max(0, piezas));
                if (s.Get(UltimoDiaPoda) != d) { s.Inc(Stat.DiasPodados); s.Set(UltimoDiaPoda, d); }
                break;
            case MiniJuegoTipo.Plaga:
                if (!CheckInProcessor.TienePlaga(r.Estado, hoy) || r.UltimoTratamiento == hoy) return;
                s.Inc(Stat.Bichos, Math.Max(0, piezas));
                break;
            case MiniJuegoTipo.Raices:
                if (!CheckInProcessor.TieneRaices(r.Estado, hoy) || r.UltimoTratamiento == hoy) return;
                s.Inc(Stat.Raices, Math.Max(0, piezas));
                break;
        }
        if (r.UltimoRiego == hoy && r.UltimaPoda == hoy && s.Get(UltimoDiaMimo) != d)
        {
            s.Inc(Stat.CuidadoCompleto);
            s.Set(UltimoDiaMimo, d);
        }
    });

    /// <summary>El tratamiento del día (plaga y/o raíces) quedó hecho.</summary>
    public void RegistrarTratamiento(Guid id) => Modificar(id, (r, _) =>
    {
        if (!CheckInProcessor.EnCuidado(r.Estado, Hoy))
            throw new InvalidOperationException("Este árbol no necesita tratamiento.");
        if (r.UltimoTratamiento == Hoy) return;
        r.UltimoTratamiento = Hoy;
        r.TratamientosEnCuidado++;
    });

    public void ResolverMiniEvento(Guid id, int bichos = 0) => Modificar(id, (r, s) =>
    {
        if (r.Estado.MiniEvento == MiniEvento.Ninguno)
            throw new InvalidOperationException("No hay ninguna emergencia en este árbol.");
        r.Estado.MiniEvento = MiniEvento.Ninguno;
        s.Inc(Stat.MiniEventos);
        s.Inc(Stat.Bichos, Math.Max(0, bichos));
        s.Set($"avisoEvento:{id}", 0);
    });

    public void Borron(Guid id) => Modificar(id, (r, _) => _procesador.Borron(r.Estado));

    public void EditarDiasDeCheat(Guid id, IEnumerable<DayOfWeek> dias) => Modificar(id, (r, _) =>
    {
        if (!CheatDaySchedule.PuedeEditar(Hoy))
            throw new InvalidOperationException("Los días libres se editan los sábados.");

        var nuevo = new Habit
        {
            Categoria = r.Habito.Categoria,
            EspecieDeArbol = r.Habito.EspecieDeArbol,
            Tipo = r.Habito.Tipo,
            DiasDeCheat = [.. dias],
        };
        CheatDaySchedule.Validar(nuevo);

        // La plantilla que rige hoy se conserva hasta mañana (domingo).
        r.Habito.DiasDeCheatAnteriores = [.. CheatDaySchedule.PlantillaVigente(r.Habito, Hoy)];
        r.Habito.DiasDeCheat = nuevo.DiasDeCheat;
        r.Habito.CambioDeCheatDesde = Hoy.AddDays(1);
    });

    public void EliminarHabito(Guid id) => store.Eliminar(Buscar(id).Habito.Id);

    // ---------- Perfil y jardineros ----------

    public Perfil Perfil() => store.Perfil();

    public void GuardarPerfil(Perfil perfil)
    {
        var limpio = perfil with
        {
            Nombre = Vacio(perfil.Nombre),
            Correo = Vacio(perfil.Correo),
            Telefono = Vacio(perfil.Telefono),
        };
        if (limpio.Problema() is { } p) throw new ArgumentException(p);
        store.GuardarPerfil(limpio);
    }

    public IReadOnlyList<Jardinero> Jardineros() => store.Jardineros();

    public Jardinero AgregarJardinero(string nombre, string telefono)
    {
        if (store.Jardineros().Count >= Jardinero.Maximo)
            throw new InvalidOperationException($"Puedes tener hasta {Jardinero.Maximo} jardineros.");
        var j = Jardinero.Nuevo(nombre, telefono);
        if (store.Jardineros().Any(x => Jardinero.Limpiar(x.Telefono) == Jardinero.Limpiar(j.Telefono)))
            throw new InvalidOperationException("Ese número ya está en tus jardineros.");
        store.GuardarJardinero(j);
        return j;
    }

    public void EliminarJardinero(Guid id) => store.EliminarJardinero(id);

    private static string? Vacio(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // ---------- Internos ----------

    /// <summary>Avance diario de todos los árboles + estadísticas de jardín + logros. Idempotente.</summary>
    private void Sincronizar()
    {
        var hoy = Hoy;
        store.EnTransaccion(() =>
        {
            var arboles = store.Cargar();
            Cambiar(s =>
            {
                foreach (var r in arboles)
                {
                    var antes = r.Estado.UltimoDiaEvaluado;
                    var eventos = AvanceDiario.Avanzar(r.Habito, r.Estado, hoy);
                    bool cambio = r.Estado.UltimoDiaEvaluado != antes;
                    foreach (var ev in eventos) Procesar(s, r, ev);

                    // Cierre de un cuidado por recaída declarada.
                    if (r.CuidadoPendiente && !CheckInProcessor.EnCuidado(r.Estado, hoy))
                    {
                        if (r.TratamientosEnCuidado > 0)
                        {
                            s.Inc(Stat.De(Stat.Cuidados, r.Habito.Categoria));
                            s.Inc(Stat.CuidadosTotales);
                            if (r.Estado.FenixPendiente) s.Set(Stat.Fenix, 1);
                        }
                        r.CuidadoPendiente = false;
                        r.Estado.FenixPendiente = false;
                        cambio = true;
                    }
                    if (_procesador.PuedeBorron(r.Estado)) s.Max(Stat.De(Stat.Borron, r.Habito.Categoria), 1);

                    ActualizarRachas(s, r);
                    if (cambio) store.Guardar(r);
                }

                var dias = arboles.Select(a => a.Habito.DiasDeCrecimiento).ToList();
                s.Max(Stat.EnFlorALaVez, dias.Count(x => x >= 7));
                s.Max(Stat.TreintaALaVez, dias.Count(x => x >= 30));
                if (dias.Count(x => x >= 365) >= 3) s.Set(Stat.HuertoEterno, 1);
                if (s.Get(Stat.TierraBaldia) > 0 && dias.Count(x => x >= 90) >= 3) s.Set(Stat.BosqueQueVuelve, 1);
            });
            EvaluarLogros();
        });
    }

    private void Procesar(Estadisticas s, HabitRecord r, EventoDelDia ev)
    {
        var id = r.Habito.Id;
        switch (ev.Tipo)
        {
            case TipoEvento.CrecioPorDiaLibre:
                s.Inc(Stat.De(Stat.Libres, r.Habito.Categoria));
                break;
            case TipoEvento.MiniEvento:
                s.Set($"avisoEvento:{id}", 1);
                break;
            case TipoEvento.StrikeAbandono:
                // La Fruta del Fénix es para una recaída declarada, no para un abandono.
                r.Estado.FenixPendiente = false;
                s.Set($"avisoPlaga:{id}", 1);
                break;
            case TipoEvento.MurioPorAbandono:
                RegistrarMuerte(s, r, ev.Valor, abandono: true);
                s.Set($"avisoMuerte:{id}", 1);
                break;
        }
    }

    private static void RegistrarMuerte(Estadisticas s, HabitRecord r, int diasAntes, bool abandono)
    {
        var cat = r.Habito.Categoria;
        int dia = r.Habito.FechaDeInicioRachaActual.DayNumber; // la muerte reinicia la fecha a ese día
        r.CuidadoPendiente = false;
        r.ConstanciaActual = 0;
        s.Inc(Stat.De(Stat.Muertes, cat));

        if (diasAntes >= 365) s.Set(Stat.LeyendaCaida, 1);
        if (abandono && diasAntes >= 180) s.Set(Stat.JardinOlvidado, 1);

        // Tierra Baldía: 3 muertes (de cualquier árbol) en menos de 14 días.
        if (Empujar(s, "muerteG", dia) is { } g && dia - g < 14) s.Set(Stat.TierraBaldia, 1);

        // Raíces de Ceniza: 3 muertes del mismo hábito en menos de 180 días.
        if (Empujar(s, $"muerteH:{cat}", dia) is { } hh && dia - hh < 180 && s.Get(Stat.Ceniza) == 0)
        {
            s.Set(Stat.Ceniza, 1);
            s.Set(CenizaHabito, (int)cat + 1);
        }
    }

    /// <summary>Guarda las últimas 3 fechas; devuelve la más vieja cuando ya hay 3.</summary>
    private static int? Empujar(Estadisticas s, string prefijo, int dia)
    {
        int a = s.Get($"{prefijo}1"), b = s.Get($"{prefijo}2");
        s.Set($"{prefijo}1", dia);
        s.Set($"{prefijo}2", a);
        s.Set($"{prefijo}3", b);
        return b > 0 ? b : null;
    }

    private void ActualizarRachas(Estadisticas s, HabitRecord r)
    {
        var cat = r.Habito.Categoria;
        int dias = r.Habito.DiasDeCrecimiento;
        s.Max(Stat.De(Stat.Racha, cat), dias);
        s.Max(Stat.RachaMax, dias);
        if (s.Get(Stat.De(Stat.Muertes, cat)) > 0)
        {
            s.Max(Stat.De(Stat.TrasMuerte, cat), dias);
            if (dias >= 7) s.Set(Stat.Replantes, 1);
        }
        if (dias >= 365 && s.Get(Stat.LeyendaCaida) > 0) s.Set(Stat.Corona, 1);
        if (dias >= 1825) s.Set(Stat.CincoInviernos, 1);
        if (dias >= 180 && s.Get(Stat.JardinOlvidado) > 0 && r.Estado.StrikesAbandono == 0) s.Set(Stat.Regreso, 1);
        if (dias >= 180 && s.Get(Stat.Ceniza) > 0 && s.Get(CenizaHabito) == (int)cat + 1) s.Set(Stat.FenixDeCeniza, 1);
    }

    private void EvaluarLogros()
    {
        var nuevos = EvaluadorLogros.Nuevos(store.Estadisticas(), store.NivelesLogro(), Hoy);
        if (nuevos.Count == 0) return;
        foreach (var n in nuevos) store.AgregarNivel(n);
        Cambiar(s => s.Inc(SinAnunciar, nuevos.Count));
    }

    private HabitRecord Buscar(Guid id) =>
        store.Cargar().FirstOrDefault(r => r.Habito.Id == id)
        ?? throw new KeyNotFoundException("Ese árbol ya no está en el jardín.");

    private void Modificar(Guid id, Action<HabitRecord, Estadisticas> cambio)
    {
        Sincronizar();
        var r = Buscar(id);
        store.EnTransaccion(() =>
        {
            Cambiar(s => cambio(r, s));
            store.Guardar(r);
            EvaluarLogros();
        });
    }

    private void Cambiar(Action<Estadisticas> accion)
    {
        var s = new Estadisticas(store.Estadisticas());
        accion(s);
        if (s.Cambios.Count > 0) store.GuardarEstadisticas(s.Cambios);
    }

    private VistaArbol Vista(HabitRecord r)
    {
        var hoy = Hoy;
        var e = r.Estado;
        var esCheat = CheatDaySchedule.EsDiaDeCheat(r.Habito, hoy);
        return new VistaArbol(
            r.Habito.Id,
            r.Habito.Categoria,
            r.Habito.EspecieDeArbol,
            r.Habito.Tipo,
            calculadora.CalcularEtapa(r.Habito),
            r.Habito.DiasDeCrecimiento,
            CheckInProcessor.TienePlaga(e, hoy),
            CheckInProcessor.TieneRaices(e, hoy),
            CheckInProcessor.FinCuidado(e, hoy),
            e.StrikesDeclarados,
            e.StrikesAbandono,
            PideCheckInHoy: !esCheat && e.FechaUltimoCheckIn != hoy,
            EsCheatDayHoy: esCheat,
            RegadoHoy: r.UltimoRiego == hoy,
            PodadoHoy: r.UltimaPoda == hoy,
            TratadoHoy: r.UltimoTratamiento == hoy,
            PuedeBorron: _procesador.PuedeBorron(e),
            DiasSinVisita: e.DiasNormalesSinCheckIn,
            MiniEvento: e.MiniEvento,
            PideAsistencia: r.Habito.ApoyoGrupo || r.Habito.ApoyoTerapia,
            AsistioHoy: r.UltimaAsistencia == hoy,
            DiasDeCheat: [.. r.Habito.DiasDeCheat]);
    }

    /// <summary>Copia de trabajo de las estadísticas que recuerda qué cambió.</summary>
    private sealed class Estadisticas(IReadOnlyDictionary<string, int> origen)
    {
        private readonly Dictionary<string, int> _valores = new(origen);
        public Dictionary<string, int> Cambios { get; } = [];

        public int Get(string k) => _valores.GetValueOrDefault(k);

        public void Set(string k, int v)
        {
            if (Get(k) == v && _valores.ContainsKey(k)) return;
            if (v == 0 && !_valores.ContainsKey(k)) return;
            _valores[k] = v;
            Cambios[k] = v;
        }

        public void Inc(string k, int n = 1) { if (n != 0) Set(k, Get(k) + n); }

        public void Max(string k, int v) { if (v > Get(k)) Set(k, v); }
    }
}
