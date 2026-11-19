# Jardín secreto, hábitos de lista y logros personalizados — Diseño

Fecha: 2026-11-19. Estado: acordado con el usuario en conversación (aprobó "continúa y desarrolla").

## Intención

La app debe ser "el compañero perfecto" para dejar un mal hábito: **discreta** (si alguien ve el teléfono solo ve un jardín), **honesta con los tiempos reales de recuperación** (basados en literatura médica, no en tiempos de videojuego) y **motivadora sin avergonzar** (la recaída es parte del proceso). Los logros funcionan como una bitácora/estante de trofeos privado del que el usuario se sienta orgulloso.

Regla transversal: **todo número de tiempo tiene fundamento médico/psicológico** (ver "Fundamentos").

## Metas / no-metas / anti-metas

- Metas: anonimato en jardín, árbol y notificaciones; selección de hábito por lista; un árbol por hábito; reglas de strikes nuevas; logros en 3 niveles + malditos/legendarios ultra secretos; notificaciones juguetonas.
- No-metas: detección automática de apps, funciones sociales, sincronización en la nube.
- Anti-metas (fracaso aunque "funcione"): que una notificación, el jardín o un logro visible delate el hábito; que un logro se gane en días cuando la recuperación real toma meses; que un logro maldito se sienta como coleccionable o excusa para recaer; que la app culpe o humille (efecto de violación de la abstinencia de Marlatt: la culpa aumenta la recaída).

## Fundamentos (fuentes consultadas)

| Tema | Dato | Fuente |
|---|---|---|
| Remisión | DSM-5: temprana ≥3 y <12 meses; sostenida ≥12 meses | DSM-5 (PMC3767415) |
| Recuperación alcohol | NIAAA: inicial ≤3 m, temprana 3–12 m, sostenida 1–5 años, estable >5 años | NIAAA |
| Abstinencia alcohol | Convulsiones 12–48 h, delirium tremens 48–72 h (hasta 7–10 d), mortal 1–4 %; síntomas residuales ≥1 año | StatPearls NBK441882, MedlinePlus |
| Benzodiacepinas | Suspender de golpe puede ser mortal; síntomas prolongados hasta >12 meses | FDA |
| Nicotina | Pico días 1–3; 1.ª semana = mayor riesgo de recaída; síntomas 3–4 semanas, a veces meses | NCI, NHS, Smokefree.gov |
| Juego | Recaída típica ~1.4 años después de recuperarse | PMC8969856 |
| Pornografía | CSBD en CIE-11; TCC con más respaldo | PMC5775124, PMC11214846 |
| Cafeína | Abstinencia 2–9 días, pico 20–51 h | Juliano & Griffiths 2004 (vía PMC) |
| Sal | Gusto se adapta en 8–12 semanas | PMC4593321 |
| Azúcar | Reducción gradual funciona | PMC11538494 |
| Hábitos | Automaticidad mediana 66 días (rango 18–254) | Lally 2010; meta-análisis PMC11641623 |
| Cambio | Etapas del cambio; la recaída es común; fumadores 3–4 intentos | StatPearls NBK556005, SAMHSA TIP 35 |
| Lapsus vs recaída | La culpa tras un desliz aumenta la recaída | Marlatt (PMC6760427) |

## Diseño

### 1. Catálogo de hábitos (lista desplegable, orden alfabético, "Otros" al final)

| Hábito | Modo |
|---|---|
| Alcohol | Elige el usuario (abstinencia total o moderar — NIAAA reconoce "dejar de beber en exceso") + aviso médico |
| Apuestas | Solo abstinencia total |
| Azúcares | Moderable |
| Cafeína | Moderable |
| Cigarro y vapeo | Elige el usuario |
| Compras impulsivas | Moderable |
| Harinas procesadas | Moderable |
| Pornografía | Solo abstinencia total |
| Redes sociales y pantallas | Moderable |
| Sal | Moderable |
| Sustancias | Solo abstinencia total + aviso médico |
| Otros | Elige el usuario |

"Moderable" = puede elegir moderar (con días libres) o dejar por completo. Cada hábito puede plantarse una sola vez a la vez (no dos árboles para "Alcohol"), salvo "Otros".

**Aviso médico** (Alcohol y Sustancias, al elegirlos): dejarlos de golpe tras uso diario puede ser peligroso; hablar con un médico; Línea de la Vida 800 911 2000 (CONASAMA, 24 h, gratis); urgencia: 911.

### 2. Plantar (pantalla nueva)

1. Hábito (lista desplegable). Sin campo de nombre.
2. Modo (solo si el hábito lo permite) + días libres si es moderable.
3. Mini cuestionario opcional:
   - "¿Ya llevas tiempo limpio comprobable (ficha, registro)?" → días/meses/años. El árbol nace con esos días de crecimiento.
   - "¿Tienes apoyo?" → grupo de apoyo / terapia (casillas). Activa el registro opcional de asistencia en el check-in.
4. Árbol: solo especies **no usadas** por otro árbol vivo del jardín. 10 especies: Manzano, Naranjo, Limonero, Cerezo, Durazno, Higuera + **Aguacate, Mango, Granado, Olivo**.

### 3. Discreción

- El jardín y la pantalla del árbol muestran **la especie**, nunca el hábito. El check-in pregunta "¿Cómo te fue hoy?".
- El hábito solo aparece dentro del **estante** (detrás del bloqueo del teléfono).
- Notificaciones: nunca nombran el hábito; solo la especie.

### 4. Crecimiento (cambia: ya no es tiempo de calendario)

El árbol tiene **días de crecimiento** acumulados. Suma 1 día por:
- un check-in "Cumplido", o
- un día libre (moderable) si el último día normal anterior tuvo check-in,

**siempre que el árbol no esté en cuidado** (plaga/raíces). Sin check-in el árbol no crece, pero la racha no se rompe. Las etapas (Semilla → … → Fruta Legendaria, 365) se calculan con estos días. Tiempo previo comprobable = días de crecimiento iniciales.

### 5. Strikes declarados ("Lo rompí")

- Se **acumulan** (no hace falta que sean seguidos) y no se borran solos.
- 1.º → **Plaga**: 3 días de cuidado (insecticida).
- 2.º → **Plaga + raíces**: plaga 3 días y raíces hasta completar **8 días** desde el strike (los cuidados se hacen a la vez; la plaga desaparece al día 3).
  - Si el 2.º llega con la plaga del 1.º aún activa: solo se agregan raíces y el cuidado termina 5 días después del fin de la plaga (strikes en días seguidos ⇒ 8 días en total).
- 3.º → el árbol **muere**: vuelve a semilla, días de crecimiento a 0, strikes a 0.
- Mientras hay cuidado el árbol no crece.

### 6. Abandono (contador aparte, mismo efecto)

Se cuentan días normales (no libres) sin check-in desde el último check-in (o desde plantar).
- Días 1–13: el árbol se pausa.
- Días 7–13: **mini-eventos** de emergencia (caracol, rama seca, tierra reseca…): llamativos, se resuelven en segundos, sin consecuencias en strikes. Una notificación juguetona.
- Día 14 → strike por abandono 1 (plaga); día 21 → 2 (plaga + raíces, 8 días); día 28 → muerte.
- El cuidado empieza en la fecha del evento (día 14/21/28), no cuando vuelve el usuario.
- Los strikes por abandono se acumulan entre ausencias y son independientes de los declarados (3 del mismo contador = muerte).

### 7. Borrón y cuenta nueva (opcional)

Se ofrece cuando pasan **90 días sin ningún strike** (declarado o por abandono) y hay strikes. Borra ambos contadores. Fundamento: DSM-5 considera "remisión temprana" a partir de 3 meses sin criterios; equivale a permitir, como mucho, un tropiezo por trimestre sin acumularlo. Reemplaza la regla de 6 meses.

### 8. Logros

Todos tienen **título con juego de palabras de jardín adecuado al logro** + **descripción honesta y directa**, y **3 niveles (★ ★★ ★★★)** del mismo reto, salvo los malditos/legendarios, que son de un solo nivel.

**Estante:** toda la pestaña de logros se abre con el bloqueo del teléfono (PIN, patrón o huella; `BiometricPrompt` con credencial del dispositivo). Al ganar un logro, en la app y fuera, solo se dice "🏆 ¡Desbloqueaste un logro! Abre tu jardín para verlo."

| Categoría | Visible antes de ganarse | Notas |
|---|---|---|
| Generales | Sí, desde el inicio | Cuidar el jardín, constancia, honestidad, pedir ayuda |
| Del hábito | Solo después de plantar ese hábito (y para siempre después) | ≥10 por hábito, tiempos médicos |
| Ocultos (mini-juegos) | **No existe nada** que los muestre hasta ganarlos | ★ ≈3 semanas de cuidado regular, ★★★ ≈3 meses |
| Malditos | Ocultos | Muy difíciles; traen un debuff de juego |
| Legendarios | Ocultos | Redención de un maldito o hazañas de ≥1 año (Fruta del Fénix) |

**Malditos:** consecuencias de eventos graves y raros (p. ej. Leyenda Caída: muere un árbol legendario). Su debuff solo afecta los mini-juegos (más bichos, raíces más tercas, tierra que se seca más rápido) — nunca alarga días reales de cuidado. Se presentan como cicatriz que se puede sanar, nunca con culpa: cada maldito tiene un legendario de **redención** que quita su debuff.

**Fruta del Fénix (legendario):** tras 365 días sin strikes, primer strike, completar el cuidado y que el árbol vuelva a crecer.

**Tiempos de los logros por hábito** (ejemplos de niveles, en días de crecimiento):
- Alcohol / Sustancias: 3·14·30 (abstinencia aguda) · 90·180·365 (remisión temprana) · 730·1095·1825 (sostenida → estable).
- Cigarro y vapeo: 3·7·28 (pico, semana crítica, 4 semanas) · 90·180·365 · …
- Apuestas: 30·90·180 · 365·510·730 (recaída típica ~1.4 años).
- Pornografía: 7·30·90 · 180·365·730.
- Cafeína: 3·9·30 (abstinencia 2–9 días) · 66·180·365.
- Sal: 28·56·84 (8–12 semanas).
- Azúcares / Harinas / Pantallas / Compras / Otros: 14·30·66 (automaticidad) · 90·180·254 · 365…
El resto de logros por hábito usan check-ins honestos, asistencia a apoyo, cuidados completados, volver a plantar, días libres respetados y semanas sin strike.

### 9. Notificaciones (canal genérico "Tu jardín")

Todas juguetonas e inofensivas a simple vista, nunca nombran el hábito:
- Recordatorio (tarde) si falta check-in: "🌧️ Tu jardín tiene sed. ¿Pasas a regarlo?" (rotan varios textos).
- Etapas: "🌸 ¡Tu durazno floreció!", "🍏 ¡Tu manzano dio sus primeros frutos!".
- Mini-eventos (días 7–13 sin entrar): "🐌 Un caracol se coló en tu limonero…".
- Plaga por abandono: "🐛 ¡Oh no! Algo le pasa a tu cerezo."
- Logro: "🏆 ¡Desbloqueaste un logro! Abre tu jardín para verlo."
- Día libre (existente, reescrito): "☀️ Hoy tu naranjo descansa. Disfruta con medida."

### 10. Datos y migración

- Esquema SQLite v2: hábito (categoría, días de crecimiento, strikes declarados/abandono, fechas de fin de plaga y raíces, apoyo), tabla de estadísticas (contadores para logros), tabla de niveles de logro, eventos de abandono procesados, debuffs activos.
- Árboles existentes (v1): categoría por coincidencia del nombre con la lista; si no coincide, "Otros". Días de crecimiento = días transcurridos actuales (no se le quita a nadie lo ya crecido). Especies repetidas existentes se conservan; la regla de unicidad aplica al plantar.
- Logros v1: Fruta Legendaria → nivel del logro equivalente; Leyenda Caída se conserva como maldito.

## Lo que queda al constructor

Textos finales de cada logro (juego de palabras + descripción), umbrales exactos dentro de los rangos médicos, mecánica concreta de cada mini-evento y de los debuffs, hora de los recordatorios (propuesta: 9:00 día libre, 20:00 recordatorio).
