# Redstone Studio — Diagramma dei Casi d'Uso

```mermaid
flowchart LR
    U(["Utente"])

    subgraph RS["Redstone Studio"]
        subgraph Editor
            UC1(["Seleziona Strumento"])
            UC2(["Posiziona Componente"])
            UC3(["Rimuovi Componente"])
            UC4(["Cambia Stato"])
            UC5(["Imposta Direzione"])
            UC6(["Annulla modifica (Undo)"])
            UC7(["Ripeti modifica (Redo)"])
        end

        subgraph Simulazione
            UC8(["Avvia simulazione"])
            UC9(["Pausa"])
            UC10(["Step singolo"])
            UC11(["Reset griglia"])
        end
    end

    U --> UC1
    U --> UC2
    U --> UC3
    U --> UC4
    U --> UC5
    U --> UC6
    U --> UC7
    U --> UC8
    U --> UC9
    U --> UC10
    U --> UC11

    UC2 -. "«include»" .-> UC1
    UC3 -. "«include»" .-> UC1
    UC4 -. "«extend»" .-> UC2
    UC9 -. "«extend»" .-> UC8
    UC6 -. "«extend»" .-> UC2
    UC6 -. "«extend»" .-> UC3
    UC7 -. "«extend»" .-> UC6
```

> **Nota:** `ChangeState()` si comporta diversamente in base al componente selezionato — Lever: toggle on/off — Button: avvia impulso — Repeater: cicla il delay da 1 a 4.
> Undo e Redo operano solo sulle modifiche alla griglia (piazzamento/rimozione/stato), non sullo stato della simulazione.
