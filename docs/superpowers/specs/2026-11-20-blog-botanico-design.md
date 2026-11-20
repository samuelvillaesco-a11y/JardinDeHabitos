# Blog botánico (parte 3) — diseño

Fecha: 2026-11-20. Estado: implementado (esquema SQLite v4, tabla `Diario`).

## Qué es

Una pestaña bajo llave con el diario del usuario y un registro diario unificado (no por árbol) de:

| Campo | Escala | Por qué |
|---|---|---|
| Ánimo | 1–5 (caras) | Autorregistro de ánimo, componente central de la TCC y la prevención de recaídas. |
| Ganas | 0–4 | Ítem de *craving* de la Escala de Abstinencia de Nicotina de Minnesota (MNWS): 0 nada … 4 muy fuerte. |
| Emociones | lista, varias | Los estados emocionales negativos son la situación de recaída más común (Marlatt y Gordon). |
| Detonantes | lista, varias | Situaciones de alto riesgo de Marlatt y Gordon: emociones negativas, conflicto con otros, presión social; más señales del entorno, hambre, cansancio (HALT). |
| Síntomas | 0–4 cada uno | Escala de la MNWS. Los ítems salen de escalas clínicas validadas (ver abajo). |
| Nota | texto libre, ≤ 4000 car. | Diario. |

Todo es opcional; una entrada vacía no se guarda. Una entrada por día; se puede editar o borrar.

## Síntomas y de dónde salen

- **Generales** (se preguntan siempre): ansiedad, irritabilidad, ánimo bajo, concentración, inquietud, mal dormir, apetito, cansancio. Son los ítems de la MNWS (criterios DSM-5 de abstinencia de tabaco) y se repiten en la abstinencia de cafeína, cannabis y en el síndrome de abstinencia del juego (DSM-5: inquietud o irritabilidad al intentar dejarlo).
- **Alcohol** (CIWA-Ar, 10 ítems): temblor, sudoración, náusea, dolor de cabeza, corazón acelerado (autonómico); alucinaciones y confusión/desorientación como señales de alarma.
- **Sustancias** (COWS, 11 ítems): sudoración, inquietud, nariz que escurre/lagrimeo/bostezos, malestar digestivo, dolor de huesos y músculos, taquicardia, temblor.
- **Cafeína** (Juliano y Griffiths 2004; DSM-5): dolor de cabeza, cansancio, ánimo bajo, concentración, síntomas tipo gripe (náusea, dolor muscular).
- **Pensamientos de hacerse daño**: se pregunta siempre.

Solo se muestran los síntomas que aplican a los árboles plantados (`CatalogoDiario.Para`).

## Alertas (sin puntajes clínicos)

La app no calcula un puntaje CIWA ni COWS (eso lo hace un clínico). Solo reacciona a señales claras:

| Registro | Respuesta |
|---|---|
| Pensamientos de hacerse daño | Crisis: Línea de la Vida (800 911 2000, 24 h), 911, jardineros. |
| Alucinaciones, confusión o convulsiones | Urgencia: 911 (posible delirium tremens o convulsiones por abstinencia). |
| Temblor, sudor o taquicardia "fuerte" o más | Hablar hoy con un médico. |

## Gráficas

- Ánimo y ganas: dos gráficas de línea separadas (escalas distintas: nunca doble eje), un punto por día, línea cortada en días sin registro, guía vertical y lectura al tocar.
- Síntomas: mapa de calor (filas = síntomas, columnas = días; por semana en el rango de 3 meses), un solo tono (miel) de apagado a brillante, con leyenda en palabras. "No anoté" (punto) ≠ "nada" (cuadro apagado).
- Fichas: días escritos, días seguidos, ganas promedio de esta semana contra la anterior.
- Colores medidos contra la parcela #33422A: hoja 4.4:1, miel 5.8:1 (marcas ≥ 3:1).

## Tiempos de abstinencia mostrados ("Lo que dice la ciencia")

| Hábito | Texto | Fuente |
|---|---|---|
| Alcohol | Empieza 6–24 h, pico 24–72 h, pasa en 5–7 días | Alcohol withdrawal syndrome: mechanisms, manifestations, and management (PMC6084325); NIAAA |
| Cigarro | Pico la primera semana, baja en 2–4 semanas | Hughes 2007; MNWS (PMC2527730); NCI/smokefree.gov |
| Cafeína | Empieza 12–24 h, pico 20–51 h, dura 2–9 días | Juliano y Griffiths 2004 |
| Opioides | Empieza 8–24 h, dura 4–10 días | COWS (NIDA); PMC2774236 |
| Hábitos sin abstinencia física | Detonantes; hábito automático ~66 días | Lally et al. 2010; Marlatt y Gordon 1985 |

## Fuentes

- CIWA-Ar: https://pmc.ncbi.nlm.nih.gov/articles/PMC6084325/ , https://pmc.ncbi.nlm.nih.gov/articles/PMC6007649/
- MNWS: https://pmc.ncbi.nlm.nih.gov/articles/PMC2527730/ , https://pubmed.ncbi.nlm.nih.gov/15969874/
- COWS: https://nida.nih.gov/sites/default/files/ClinicalOpiateWithdrawalScale.pdf , https://pmc.ncbi.nlm.nih.gov/articles/PMC2774236/
- TCC y autorregistro: https://pmc.ncbi.nlm.nih.gov/articles/PMC9948631/
- Prevención de recaídas con registro diario de ganas y ánimo: https://pmc.ncbi.nlm.nih.gov/articles/PMC4489711/ , https://pmc.ncbi.nlm.nih.gov/articles/PMC3408809/

## Logro

"Cuaderno de raíces" (general): escribir en el blog 7 / 30 / 66 días distintos (66 = mediana de Lally 2010 para volver automático un hábito). Borrar entradas no quita el logro.
