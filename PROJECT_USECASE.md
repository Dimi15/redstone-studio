# Redstone Studio — Diagramma dei Casi d'Uso

```mermaid
flowchart LR
    U(["👤 Utente"])

    subgraph RS["Redstone Studio"]
        subgraph Editor
            UC1(["Seleziona Strumento"])
            UC2(["Posiziona Componente"])
            UC3(["Rimuovi Componente"])
            UC4(["Cambia Stato"])
            UC5(["Imposta Direzione"])
        end

        subgraph Simulazione
            UC6(["Avvia simulazione"])
            UC7(["Pausa"])
            UC8(["Step singolo"])
            UC9(["Reset griglia"])
        end

        subgraph File
            UC10(["Salva circuito (JSON)"])
            UC11(["Carica circuito (JSON)"])
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
    UC7 -. "«extend»" .-> UC6
```

> **Nota:** `ChangeState()` si comporta diversamente in base al componente selezionato — Lever: toggle on/off — Button: avvia impulso — Repeater: cicla il delay da 1 a 4
