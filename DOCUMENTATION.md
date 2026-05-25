# Redstone Studio — Documentazione del Progetto

**Titolo:** Redstone Studio — Simulatore di Circuiti Redstone  
**Classe:** 4J  
**Anno Scolastico:** A.S. 2025/2026

**Componenti del gruppo:**
- Dmytriy Kovalyshyn
- Filippo Tognoni
- Leonida Biondi
- Luca Pischettola

---

## Indice

1. [Introduzione](#1-introduzione)
2. [Analisi](#2-analisi)
3. [Progettazione della GUI](#3-progettazione-della-gui)
4. [Problemi Riscontrati](#4-problemi-riscontrati)
5. [Implementazione](#5-implementazione)

---

## 1. Introduzione

Il progetto "Redstone Studio" nasce dall'idea di riprodurre in forma semplificata il comportamento dei circuiti redstone presenti nel videogioco Minecraft. Nel gioco, il redstone è un materiale che si comporta in modo analogo a un filo elettrico: trasmette segnali tra componenti che possono agire come interruttori, amplificatori, invertitori e indicatori luminosi. L'insieme di questi elementi consente di costruire circuiti logici relativamente complessi, paragonabili a quelli studiati in elettronica.

L'obiettivo del progetto è realizzare un'applicazione desktop leggera che permetta all'utente di disporre questi componenti su una griglia bidimensionale, di eseguire la simulazione del circuito tick per tick e di salvare o ricaricare la configurazione tramite un file JSON. L'applicazione non pretende di replicare fedelmente tutti i meccanismi del gioco originale, ma ne cattura i comportamenti fondamentali sufficienti per costruire semplici circuiti logici.

Il progetto è stato sviluppato in C# con .NET 8, utilizzando il framework Avalonia UI per l'interfaccia grafica e il pattern architetturale MVVM. La struttura generale del codice segue i principi della Clean Architecture, mantenendo separati il livello di dominio, quello applicativo, quello infrastrutturale e quello di presentazione.

---

## 2. Analisi

### 2.1 Requisiti Funzionali

| ID | Requisito |
|----|-----------|
| RF-01 | L'utente deve poter selezionare un componente dalla barra degli strumenti e posizionarlo cliccando su una cella della griglia. |
| RF-02 | Cliccando di nuovo sulla stessa cella con lo stesso strumento selezionato, il componente deve cambiare il proprio stato interno (accendere una leva, attivare un pulsante, cambiare il ritardo di un ripetitore). |
| RF-03 | L'utente deve poter rimuovere un componente selezionando lo strumento "Cancella" e cliccando sulla cella. |
| RF-04 | I componenti che ammettono una direzione (torcia, ripetitore) devono essere posizionabili nelle quattro direzioni cardinali tramite i pulsanti di rotazione. |
| RF-05 | Il motore di simulazione deve propagare correttamente il segnale redstone secondo le regole: decadimento del filo, inversione della torcia, ritardo del ripetitore. |
| RF-06 | L'utente deve poter avviare (Play), mettere in pausa (Pause), avanzare di un singolo tick (Step) e azzerare la simulazione (Reset). |
| RF-07 | L'applicazione deve permettere di salvare il circuito corrente su un file `.json` e di ricaricarlo successivamente. |

### 2.2 Requisiti Non Funzionali

- **Semplicità:** Il codice deve essere leggibile e non sovra-ingegnerizzato; si evitano pattern enterprise non necessari.
- **Robustezza:** Il progetto deve compilare senza errori ad ogni modifica.
- **Portabilità:** Essendo basato su Avalonia UI e .NET 8, l'applicazione è compatibile con Windows, macOS e Linux.
- **Manutenibilità:** La Clean Architecture garantisce che ogni livello possa essere modificato indipendentemente dagli altri.

### 2.3 Componenti Implementati

| Componente | Simbolo | Descrizione |
|---|:---:|---|
| Wire | `·` | Filo redstone. Trasmette il segnale con decadimento di 1 per blocco (max 15). |
| Lever | `│`/`╱` | Leva on/off. Sorgente di segnale permanente finché accesa. |
| Button | `●`/`○` | Pulsante. Emette un impulso di durata fissa (3 tick) e poi si spegne automaticamente. |
| RedstoneBlock | `■` | Blocco redstone. Sorgente costante di segnale (potenza 15). |
| Torch | `↑`/`↓` | Torcia redstone. Invertitore: accesa se il suo ingresso non è alimentato, spenta altrimenti. |
| Repeater | `▲▶▼◀` | Ripetitore. Amplifica il segnale a 15 e introduce un ritardo configurabile da 1 a 4 tick. |
| RedstoneLamp | `✦`/`✧` | Lampada redstone. Si illumina quando riceve segnale. Nodo terminale: non trasmette oltre. |
| Block | ` ` | Blocco solido non conduttivo. Utile come barriera o supporto visivo. |

---

## 3. Progettazione della GUI

L'interfaccia è stata progettata con un tema scuro ispirato all'estetica di Minecraft, usando come colori di base il nero (`#111111`) e il grigio scuro per i pannelli (`#1A1A1A`, `#1C1C1C`).

### Layout Generale

La finestra è strutturata con un `DockPanel`:

```
┌─────────────────────────────────────────────┐
│  ⚡ Redstone Studio           [header]       │
├─────────────────────────────────────────────┤
│  [Toolbar: strumenti + direzioni + stato]   │
├─────────────────────────────────────────────┤
│                                             │
│         Griglia 20×15 (ScrollViewer)        │
│                                             │
├─────────────────────────────────────────────┤
│  [▶ Play] [⏸ Pause] [⏭ Step] [⏹ Reset]    │
│  [💾 Salva] [📂 Carica]      Tick: 42       │
└─────────────────────────────────────────────┘
```

### Griglia

La griglia è realizzata con un `ItemsControl` legato alla `ObservableCollection<CellViewModel>`. Il pannello degli item è un `UniformGrid` con numero di colonne pari a `GridWidth` (20). Ogni cella è un `Button` 36×36 px con sfondo colorato in base al tipo di entità e al livello di segnale, e un simbolo Unicode centrato.

### Codice Colori

| Componente | Off | On / Alimentato |
|---|---|---|
| Air | `#181818` | — |
| Wire (livello n) | `#2A0000` | gradiente `#280000` → `#FF3C00` |
| Lever | `#666666` | `#CC9900` |
| Button | `#774422` | `#DD4400` |
| RedstoneBlock | `#BB0000` (fisso) | — |
| Torch | `#552200` | `#CC4400` |
| Repeater | `#442200` | `#883300` |
| RedstoneLamp | `#333300` | `#DDAA00` |
| Block | `#4A4A4A` (fisso) | — |

### Binding e Compilazione

Il progetto usa `AvaloniaUseCompiledBindingsByDefault=true`, ma il `DataTemplate` della griglia usa `x:CompileBindings="False"` per evitare problemi con i binding dinamici. Il pannello items usa anch'esso `x:CompileBindings="False"` per il binding `Columns="{Binding GridWidth}"`.

---

## 4. Problemi Riscontrati

### 4.1 Doppio Decadimento del Segnale

**Problema:** Nella prima versione del BFS, il segnale decadeva di 2 per ogni passo invece di 1. Il motivo era duplice: il BFS accodava il nodo successivo con `power - 1`, e poi `Wire.UpdatePower` sottraeva un altro 1 al valore ricevuto.

**Soluzione:** `Wire.UpdatePower` ora salva il valore ricevuto direttamente, senza ulteriori sottrazioni. Il decadimento è interamente gestito dal BFS che passa `power - 1` ai nodi vicini.

```csharp
public void UpdatePower(SignalStrength incomingPower)
{
    CurrentPower = incomingPower;
}
```

### 4.2 BFS che non Propagava il Segnale

**Problema:** Le sorgenti di potenza (Lever, RedstoneBlock, Button) venivano inserite nel BFS come posizioni di partenza. Quando il BFS le estraeva dalla coda, l'entità presente era una Lever (o simile), non un Wire. Poiché il BFS gestiva solo Wire, terminava immediatamente senza mai propagare il segnale ai vicini, lasciando il circuito sempre spento.

**Soluzione:** `CollectSources()` ora non accoda la posizione della sorgente stessa, bensì le quattro celle adiacenti a potenza 15. Così il BFS inizia già dai fili che circondano la sorgente, senza dover riconoscere il tipo della sorgente stessa.

```csharp
private Queue<(Position, int)> CollectSources()
{
    var queue = new Queue<(Position, int)>();
    foreach (var pos in _board.AllPositions())
    {
        var e = _board.GetEntity(pos);
        if (e is (Lever or RedstoneBlock or Button) && e.IsPowering())
            EnqueueNeighbors(pos, 15, queue);
    }
    return queue;
}
```

### 4.3 Lampada Non Illuminata all'Ultimo Salto

**Problema:** Il BFS propagava il segnale con decadimento di 1 per ogni salto. Una lampada adiacente a un filo con potenza 1 riceveva `power - 1 = 0` e non si accendeva, anche se era fisicamente connessa a un circuito alimentato.

**Soluzione:** Le lampade sono state rimosse dal BFS e gestite in un passo separato `ResolveLamps()`, chiamato al termine di ogni tick. Questo metodo usa `MaxNeighborPower(pos)` per leggere il segnale direttamente dai vicini senza decrementarlo, garantendo l'accensione corretta.

```csharp
private void ResolveLamps()
{
    foreach (var pos in _board.AllPositions())
    {
        if (_board.GetEntity(pos) is not RedstoneLamp lamp) continue;
        lamp.UpdatePower(new SignalStrength(MaxNeighborPower(pos)));
    }
}
```

### 4.4 Torcia che Non Propagava il Segnale ai Fili

**Problema:** `ResetAllPower()` chiama `UpdatePower(SignalStrength.Min)` su ogni `IPowerable`, incluse le torce. Poiché `Min.Value = 0` e la torcia è un invertitore (`_on = incomingPower.Value == 0`), questa chiamata metteva ogni torcia nello stato ON all'inizio di ogni tick. La logica `ResolveTorches()` confrontava poi lo stato post-reset (sempre ON) con lo stato finale (ancora ON, se non c'era input) e non rilevava nessun cambiamento. Di conseguenza, il secondo BFS flood dalle torce non veniva mai eseguito, e i fili adiacenti alle torce rimanevano a potenza 0.

**Soluzione:** Il secondo BFS flood dalle torce viene ora eseguito incondizionatamente, non solo in caso di cambio di stato. `CollectTorchSources()` include già solo le torce accese, quindi se nessuna torcia è attiva la coda è vuota e il BFS è un no-op.

```csharp
// Prima (difettoso):
if (ResolveTorches())
    BfsFlood(CollectTorchSources());

// Dopo (corretto):
ResolveTorches();
BfsFlood(CollectTorchSources());
```

### 4.5 Stato Stale della Torcia rispetto ai Fili

**Problema:** La torcia leggeva il livello di segnale dei vicini prima che il BFS li avesse aggiornati, risultando in un'inversione basata su dati obsoleti (tutti i fili erano ancora a 0 dopo il reset).

**Soluzione:** Il tick è suddiviso in fasi ordinate: prima il BFS propaga il segnale dai generatori attraverso i fili, poi `ResolveTorches()` aggiorna le torce sulla base dei fili già aggiornati, infine il secondo BFS porta il segnale delle torce ai fili successivi.

### 4.6 Visited HashSet che Blocca Cammini a Potenza Maggiore

**Problema:** Il BFS usava un `HashSet<Position>` per evitare di rivisitare i nodi. In un grafo in cui la stessa cella può essere raggiunta da due percorsi, il HashSet impediva al segnale più forte di sovrascrivere quello più debole già assegnato.

**Soluzione:** Il HashSet è stato eliminato. La condizione di stop è ora basata esclusivamente sul confronto `power <= wire.CurrentPower.Value`: se il nuovo segnale non è superiore a quello già presente, la propagazione si ferma. Poiché il segnale decresce strettamente ad ogni passo, non si creano cicli infiniti.

### 4.7 Binding Compilati e Ancestor Lookup

**Problema:** Avalonia non supporta `$parent[Window].DataContext` nei binding compilati (`x:CompileBindings="True"`). Questo rendeva impossibile collegare il comando di clic di ogni cella al `MainWindowViewModel` direttamente dal XAML del `DataTemplate`.

**Soluzione:** Ogni `CellViewModel` riceve nel costruttore un delegato `Action<CellViewModel>` che punta al metodo `OnCellClicked` del `MainWindowViewModel`. Il `ClickCommand` della cella esegue semplicemente `_onClicked(this)`, eliminando completamente la necessità di risalire l'albero visuale.

### 4.8 `[RelayCommand]` su Metodi Async con Parametri

**Problema:** Il source generator di CommunityToolkit.MVVM generava l'errore `MVVMTK0007` su metodi `public async Task SaveCircuit(string path)` decorati con `[RelayCommand]`.

**Soluzione:** I metodi `SaveCircuit` e `LoadCircuit` sono stati resi semplici metodi pubblici `async Task`, chiamati direttamente dal code-behind dopo che il file picker nativo ha restituito il percorso.

### 4.9 `ImplicitUsings` Assente nel Progetto UI

**Problema:** Il file `redstoneStudio.UI.csproj` non include `<ImplicitUsings>enable</ImplicitUsings>`. Questo causava errori di compilazione su `Task`, `Action<T>`, `Func<T>` e altri tipi di sistema nelle classi del livello UI.

**Soluzione:** Tutti i file del livello UI includono esplicitamente `using System;` e `using System.Threading.Tasks;`.

### 4.10 Gestione del Timer su Thread Non UI

**Problema:** Richiamare `Tick()` da un thread di background provocava eccezioni di accesso concorrente alla `ObservableCollection<CellViewModel>`, che in Avalonia deve essere modificata esclusivamente dal thread UI.

**Soluzione:** Il timer usato per la modalità Play è un `Avalonia.Threading.DispatcherTimer`, che garantisce che il callback venga eseguito sul thread UI eliminando la necessità di dispatcher manuali o lock.

### 4.11 Deserializzazione JSON con Campi Nullable

**Problema:** Durante il caricamento di un circuito JSON, alcuni campi opzionali (`Direction`, `Delay`, `IsOn`) erano assenti per certi tipi di entità. `System.Text.Json` produceva `NullReferenceException` quando si tentava di accedere a questi valori senza verificarne la nullabilità.

**Soluzione:** Tutti i campi opzionali nei DTO sono stati dichiarati come nullable (`string?`, `int?`, `bool?`). Il metodo `ToEntity` usa l'operatore null-coalescing per fornire valori di default sicuri.

### 4.12 Ripetitore che Non Propagava il Segnale Oltre la Prima Cella

**Problema:** Quando il ritardo di un ripetitore si esauriva, il codice chiamava direttamente `w.UpdatePower(new SignalStrength(15))` sulla singola cella Wire immediatamente davanti al ripetitore e si fermava. Nessun BFS flood partiva da quella cella, quindi i fili successivi nella catena non venivano mai alimentati e la lampada in fondo al percorso rimaneva spenta.

**Soluzione:** `ResolveRepeaters()` ora restituisce una `Queue<(Position, int)>` di seed BFS invece di aggiornare direttamente i fili. Ogni volta che un ritardo si esaurisce, la posizione di uscita viene accodata a potenza 15. `Tick()` poi chiama `BfsFlood(ResolveRepeaters())`, propagando il segnale attraverso l'intera catena di fili esattamente come farebbe una sorgente normale.

```csharp
// Prima (difettoso):
private void ResolveRepeaters()
{
    // ...
    var outEntity = _board.GetEntity(outPos);
    if (outEntity is Wire w) w.UpdatePower(new SignalStrength(15)); // si ferma qui
}

// Dopo (corretto):
private Queue<(Position, int)> ResolveRepeaters()
{
    // ...
    floodSeeds.Enqueue((outPos, 15)); // BFS propaga oltre
    // ...
    return floodSeeds;
}
// In Tick():
BfsFlood(ResolveRepeaters());
```

### 4.13 Stato della Leva Non Ripristinato al Caricamento

**Problema:** Il metodo `ToEntity` nel repository creava sempre `new Lever()`, che ha il costruttore con `_on = false`. Salvare un circuito con una leva accesa e ricaricarlo produceva una leva spenta, rendendo il caricamento inaffidabile.

**Soluzione:** Il costruttore di `Lever` è stato esteso con un parametro opzionale `bool on = false`. `ToEntity` ora passa il valore salvato: `new Lever(cell.IsOn ?? false)`.

### 4.14 Ritardo del Ripetitore Non Ripristinato al Caricamento

**Problema:** Analogamente alla leva, `ToEntity` creava `new Repeater(dir)` ignorando il campo `Delay` salvato nel JSON. Un ripetitore con ritardo 3 tornava sempre a ritardo 1 dopo il caricamento.

**Soluzione:** Il costruttore di `Repeater` è stato esteso con un parametro opzionale `int delay = 1`. `ToEntity` ora passa il valore salvato: `new Repeater(dir, cell.Delay ?? 1)`. Il valore viene anche validato con `Math.Clamp(delay, 1, 4)` nel costruttore.

---

## 5. Implementazione

### 5.1 Struttura del Progetto e Clean Architecture

Il progetto è diviso in quattro layer secondo i principi della Clean Architecture. La regola fondamentale è che le dipendenze puntano sempre verso l'interno: i layer esterni conoscono quelli interni, ma non viceversa.

```
redstoneStudio.UI              →  Avalonia MVVM: ViewModels, View AXAML, code-behind
RedstoneStudio.Application     →  Motore BFS (TickSimulator), griglia (CircuitBoard)
RedstoneStudio.Domain          →  Entità, interfacce, value objects, enum
RedstoneStudio.Infrastructure  →  Persistenza JSON (JsonCircuitRepository, DTO)
```

Ogni layer ha una responsabilità precisa:
- **Domain** non dipende da nessun altro layer ed è il cuore della logica di dominio.
- **Application** dipende solo da Domain e orchestra la simulazione.
- **Infrastructure** dipende da Application e Domain per serializzare/deserializzare lo stato.
- **UI** dipende da tutti gli altri layer, ma non espone nulla verso il basso.

Tutti i namespace del codice sorgente usano la convenzione `redstone_stone.*`, indipendentemente dal nome del progetto `.csproj`.

### 5.2 Il Ciclo di Tick — `TickSimulator.Tick()`

Il metodo `Tick()` è il punto centrale dell'intera simulazione. Ogni volta che viene chiamato (manualmente tramite Step o automaticamente dal timer in Play), esegue sei fasi in ordine preciso:

```csharp
public void Tick()
{
    TickCount++;

    AdvanceTickables();          // 1. Button: decrementa countdown
    ResetAllPower();             // 2. Azzera tutti i segnali IPowerable
    BfsFlood(CollectSources());  // 3. BFS dai generatori permanenti
    ResolveTorches();            // 4. Torce: inversione basata sui fili aggiornati
    BfsFlood(CollectTorchSources()); // 5. BFS dai fili illuminati dalle torce
    ResolveRepeaters();          // 6. Coda di ritardo ripetitori
    ResolveLamps();              // 7. Accensione lampade in base ai vicini
}
```

L'ordine delle fasi è critico: se ResolveTorches venisse chiamato prima del primo BfsFlood, leggerebbe potenze di filo pari a 0 (appena azzerati) e produrrebbe risultati errati. Se ResolveLamps venisse chiamato prima di ResolveRepeaters, non vedrebbe la potenza iniettata dai ripetitori nel tick corrente.

### 5.3 `AdvanceTickables()` — Countdown dei Componenti Temporizzati

```csharp
private void AdvanceTickables()
{
    foreach (var pos in _board.AllPositions())
        if (_board.GetEntity(pos) is ITickable t) t.Tick();
}
```

Questo metodo scorre l'intera griglia cercando entità che implementano `ITickable`. Attualmente solo `Button` implementa questa interfaccia. Ogni volta che `Tick()` viene chiamato sul pulsante, il suo contatore interno (`_ticksLeft`) viene decrementato di 1. Quando il contatore raggiunge 0, `IsPowering()` restituisce `false` e il pulsante smette di essere una sorgente di segnale nella fase successiva.

Il fatto che `AdvanceTickables()` venga chiamato **prima** di `ResetAllPower()` è intenzionale: garantisce che il Button decremente il suo countdown nello stesso tick in cui emette l'ultimo segnale, impedendo che rimanga acceso un tick in più del dovuto.

### 5.4 `ResetAllPower()` — Azzeramento del Circuito

```csharp
private void ResetAllPower()
{
    foreach (var pos in _board.AllPositions())
        if (_board.GetEntity(pos) is IPowerable p) p.UpdatePower(SignalStrength.Min);
}
```

Prima di ricalcolare il circuito da zero, tutte le entità `IPowerable` vengono azzerati passando `SignalStrength.Min` (valore 0). Questo garantisce che i segnali del tick precedente non influenzino quello corrente: se una leva viene spenta, i fili che alimentava tornano a potenza 0 prima che qualsiasi altra logica venga eseguita.

Un effetto collaterale importante: `UpdatePower(0)` sulle torce le porta allo stato ON (poiché la logica di inversione è `_on = incomingPower.Value == 0`). Questo stato è temporaneo e viene corretto nella fase `ResolveTorches()`, ma ha implicazioni sulla gestione del BFS delle torce descritta nella sezione 5.7.

### 5.5 `CollectSources()` e `EnqueueNeighbors()` — Raccolta dei Seed BFS

```csharp
private Queue<(Position, int)> CollectSources()
{
    var queue = new Queue<(Position, int)>();
    foreach (var pos in _board.AllPositions())
    {
        var e = _board.GetEntity(pos);
        if (e is (Lever or RedstoneBlock or Button) && e.IsPowering())
            EnqueueNeighbors(pos, 15, queue);
    }
    return queue;
}

private void EnqueueNeighbors(Position pos, int power, Queue<(Position, int)> queue)
{
    foreach (Direction dir in Enum.GetValues<Direction>())
    {
        var n = pos.Move(dir);
        if (_board.IsInBounds(n)) queue.Enqueue((n, power));
    }
}
```

`CollectSources()` identifica tutte le sorgenti attive non-torcia (Lever accesa, RedstoneBlock sempre attivo, Button con countdown > 0) e usa `EnqueueNeighbors()` per aggiungere alla coda BFS le quattro celle adiacenti a potenza 15. Questa scelta è fondamentale: le sorgenti stesse non sono `Wire` e non verrebbero gestite dal BFS. Partire direttamente dai vicini elimina il problema alla radice.

`EnqueueNeighbors()` è un helper riutilizzato anche da `CollectTorchSources()`. Scorre le quattro direzioni cardinali, converte ogni direzione in una posizione tramite `Position.Move(Direction)`, verifica i limiti con `IsInBounds()` e accoda la coppia `(posizione, potenza)`.

### 5.6 `BfsFlood()` — Propagazione BFS del Segnale

```csharp
private void BfsFlood(Queue<(Position pos, int power)> seeds)
{
    while (seeds.Count > 0)
    {
        var (current, power) = seeds.Dequeue();
        if (!_board.IsInBounds(current) || power <= 0) continue;

        if (_board.GetEntity(current) is not Wire wire) continue;
        if (power <= wire.CurrentPower.Value) continue;

        wire.UpdatePower(new SignalStrength(power));

        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            var next = current.Move(dir);
            if (_board.IsInBounds(next))
                seeds.Enqueue((next, power - 1));
        }
    }
}
```

Il BFS processa solo celle `Wire`. La logica di terminazione usa tre condizioni in cascata:
1. `power <= 0` — il segnale è esaurito, non vale la pena propagare ulteriormente.
2. Entità non è un Wire — i blocchi non conduttivi (Block, Air, Lever) interrompono la propagazione.
3. `power <= wire.CurrentPower.Value` — il Wire ha già una potenza uguale o maggiore da un percorso più corto; non aggiornare.

La terza condizione è la chiave che rende sicuro il BFS senza un `HashSet` di visitati. Poiché il segnale decresce strettamente di 1 ad ogni salto, non si possono creare cicli in cui la potenza aumenta. Il percorso con segnale massimo aggiorna sempre per primo il Wire; tutti i percorsi successivi a potenza inferiore vengono silenziosamente ignorati.

Quando un Wire riceve un segnale più alto di quello attuale, chiama `wire.UpdatePower(new SignalStrength(power))` — che memorizza il valore direttamente, senza ulteriori decrementi — e poi accoda tutti e quattro i vicini con `power - 1`.

### 5.7 `ResolveTorches()` — Inversione delle Torce

```csharp
private void ResolveTorches()
{
    foreach (var pos in _board.AllPositions())
    {
        if (_board.GetEntity(pos) is not Torch torch) continue;
        int maxNeighborPower = MaxNeighborPower(pos);
        ((IPowerable)torch).UpdatePower(new SignalStrength(maxNeighborPower));
    }
}
```

Dopo che il primo BFS ha stabilizzato le potenze di tutti i Wire, `ResolveTorches()` valuta ogni torcia in base alla potenza massima dei suoi vicini diretti. La logica di inversione è incapsulata nella torcia stessa: `UpdatePower(0)` la accende, `UpdatePower(n > 0)` la spegne.

Un dettaglio cruciale: `ResolveTorches()` viene chiamata **incondizionatamente**, e il secondo BFS (`BfsFlood(CollectTorchSources())`) viene sempre eseguito, anche se nessuna torcia ha cambiato stato. Il motivo è che `ResetAllPower()` chiama `UpdatePower(0)` su ogni torcia, il che le porta tutte allo stato ON prima che ResolveTorches abbia modo di rileggere i fili aggiornati. Se si tentasse di rilevare un "cambio di stato" confrontando prima e dopo, il confronto sarebbe sempre falso (ON → ON) anche per torce che dovrebbero rimanere accese, e il flood verrebbe saltato. La soluzione è semplice: fare sempre il flood e lasciare che `CollectTorchSources()` filtri le torce effettivamente accese.

### 5.8 `CollectTorchSources()` — Seed BFS per le Torce

```csharp
private Queue<(Position, int)> CollectTorchSources()
{
    var queue = new Queue<(Position, int)>();
    foreach (var pos in _board.AllPositions())
        if (_board.GetEntity(pos) is Torch t && t.IsOn)
            EnqueueNeighbors(pos, 15, queue);
    return queue;
}
```

Funziona esattamente come `CollectSources()`, ma filtra le torce accese invece delle sorgenti permanenti. Usa lo stesso helper `EnqueueNeighbors()` per accodare i quattro vicini a potenza 15. Se nessuna torcia è accesa, restituisce una coda vuota e il secondo BfsFlood termina immediatamente senza effetti collaterali.

### 5.9 `ResolveRepeaters()` — Coda di Ritardo dei Ripetitori

```csharp
private Queue<(Position, int)> ResolveRepeaters()
{
    var next = new List<(Position, Direction, int)>();
    var floodSeeds = new Queue<(Position, int)>();

    // Decrementa la coda dei ripetitori in attesa
    while (_pendingRepeaters.Count > 0)
    {
        var (pos, dir, ticksLeft) = _pendingRepeaters.Dequeue();
        if (ticksLeft > 1)
            next.Add((pos, dir, ticksLeft - 1));
        else
        {
            // Delay elapsed — seed a BFS flood from the output cell at full power.
            var outPos = pos.Move(dir);
            if (_board.IsInBounds(outPos))
                floodSeeds.Enqueue((outPos, 15));
        }
    }

    // Accoda nuovi output per i ripetitori che ricevono segnale questo tick
    foreach (var pos in _board.AllPositions())
    {
        if (_board.GetEntity(pos) is not Repeater rep) continue;
        var inputPos = pos.Move(Opposite(rep.Output));
        int inputPower = 0;
        if (_board.IsInBounds(inputPos))
        {
            var src = _board.GetEntity(inputPos);
            inputPower = src switch
            {
                IPowerable pw              => pw.CurrentPower.Value,
                Lever l when l.IsOn        => 15,
                RedstoneBlock              => 15,
                Button b when b.IsPowering() => 15,
                _                          => 0
            };
        }
        ((IPowerable)rep).UpdatePower(new SignalStrength(inputPower));
        if (rep.IsPowering())
            next.Add((pos, rep.Output, rep.Delay));
    }

    foreach (var item in next)
        _pendingRepeaters.Enqueue(item);

    return floodSeeds;
}
```

I ripetitori usano una coda persistente (`_pendingRepeaters`) che sopravvive tra un tick e l'altro. Ogni voce è una tripla `(posizione, direzioneOutput, ticksRimanenti)`.

La logica si divide in due parti. Nella prima, si scorre la coda: le voci con più di 1 tick vengono riaccodate con il countdown decrementato; le voci con 1 tick hanno completato il ritardo e accodano `(outPos, 15)` nei seed BFS. Nella seconda parte si scorrono tutti i Repeater sulla griglia: si legge la potenza dalla cella di ingresso (lato opposto all'uscita via `Opposite(rep.Output)`), si aggiorna lo stato del ripetitore, e se sta ricevendo segnale si accoda una nuova voce con il ritardo.

Il metodo restituisce la coda di seed BFS, che `Tick()` passa direttamente a `BfsFlood()`. Questo garantisce che il segnale si propaghi attraverso l'intera catena di fili oltre il ripetitore, e non si fermi alla prima cella adiacente.

Il `delay` del Repeater va da 1 a 4 tick ed è ciclabile tramite `ChangeState()`. Il costruttore accetta ora anche un parametro `delay` opzionale per ripristinare correttamente il valore al caricamento dal JSON.

### 5.10 `ResolveLamps()` — Accensione Finale delle Lampade

```csharp
private void ResolveLamps()
{
    foreach (var pos in _board.AllPositions())
    {
        if (_board.GetEntity(pos) is not RedstoneLamp lamp) continue;
        lamp.UpdatePower(new SignalStrength(MaxNeighborPower(pos)));
    }
}
```

Le lampade vengono deliberatamente escluse dal BFS per evitare il problema del "decadimento all'ultimo salto": se un Wire a potenza 1 propagasse al vicino con `power - 1 = 0`, la lampada non si accenderebbe mai. Risolvere le lampade in un passo separato, leggendo direttamente la potenza dei vicini tramite `MaxNeighborPower()`, garantisce che una lampada si illumini ogni volta che è adiacente a qualcosa con potenza > 0, indipendentemente dalla posizione nella catena.

`ResolveLamps()` è l'ultima fase del tick, chiamata dopo `ResolveRepeaters()`, così può vedere anche il segnale appena iniettato da un ripetitore che ha completato il suo ritardo.

### 5.11 `MaxNeighborPower()` — Potenza Massima dai Vicini

```csharp
private int MaxNeighborPower(Position pos)
{
    int max = 0;
    foreach (Direction dir in Enum.GetValues<Direction>())
    {
        var np = pos.Move(dir);
        if (!_board.IsInBounds(np)) continue;
        var e = _board.GetEntity(np);
        int p = e switch
        {
            IPowerable pw              => pw.CurrentPower.Value,
            Lever l when l.IsOn        => 15,
            RedstoneBlock              => 15,
            Button b when b.IsPowering()=> 15,
            _                          => 0
        };
        if (p > max) max = p;
    }
    return max;
}
```

Questo helper è condiviso tra `ResolveTorches()` e `ResolveLamps()`. Per ogni vicino della posizione data, estrae la potenza usando un pattern matching che distingue tra `IPowerable` (Wire, Torch, Repeater con `CurrentPower`) e sorgenti dirette (Lever, RedstoneBlock, Button, che non implementano `IPowerable` ma emettono sempre 15 quando attive). Restituisce il massimo trovato tra tutti i quattro vicini.

### 5.12 `CircuitBoard` — La Griglia 2D

```csharp
public class CircuitBoard
{
    private Entity[,] _circuitBoard { get; }
    public int Width  => _circuitBoard.GetLength(1);
    public int Height => _circuitBoard.GetLength(0);

    public CircuitBoard(int width, int height)
    {
        _circuitBoard = new Entity[height, width];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _circuitBoard[y, x] = new Air();
    }
}
```

`CircuitBoard` è un wrapper attorno a un array bidimensionale di `Entity`. L'array è indicizzato come `[Y, X]` (riga-prima) per allinearsi alla convenzione matematica delle matrici, ma le proprietà `Width` e `Height` sono esposte con i nomi intuitivi. La griglia viene inizializzata riempiendo ogni cella con `new Air()` nel costruttore.

I metodi principali sono:
- `GetEntity(Position)` — restituisce l'entità alla posizione data.
- `PlaceEntity(Entity, Position)` — sostituisce la cella con la nuova entità.
- `RemoveEntity(Position)` — reinserisce `new Air()` nella cella, equivalente a cancellare.
- `IsInBounds(Position)` — verifica i limiti prima di qualsiasi accesso per evitare `IndexOutOfRangeException`.
- `AllPositions()` — iteratore con `yield return` che scorre tutte le posizioni in ordine riga-prima; usato da `TickSimulator` per esaminare l'intera griglia senza esporre l'array interno.

### 5.13 Entità del Dominio

Tutte le entità ereditano dalla classe astratta `Entity` e implementano solo le interfacce pertinenti al loro comportamento.

**`Air`** e **`Block`** non implementano interfacce aggiuntive e non hanno comportamento: `Air` rappresenta una cella vuota, `Block` un ostacolo solido non conduttivo.

**`Wire`** implementa `IPowerable` e `IConnectable`. Memorizza `CurrentPower` come proprietà e in `UpdatePower` salva il valore ricevuto direttamente (nessun decremento interno). `CanConnectTo` restituisce sempre `true` perché i fili si connettono in tutte le direzioni.

**`Lever`** è una sorgente on/off senza interfacce aggiuntive. Il campo `_on` viene invertito da `ChangeState()`. `IsOn` è esposta come proprietà pubblica per la serializzazione. Il costruttore accetta un parametro opzionale `bool on = false` usato da `JsonCircuitRepository` per ripristinare lo stato al caricamento.

**`Button`** implementa `ITickable`. Il costruttore accetta una durata dell'impulso (default 3 tick). `ChangeState()` riavvia il countdown impostando `_ticksLeft = _pulseDuration`. `IsPowering()` restituisce `true` finché `_ticksLeft > 0`. Ogni chiamata a `Tick()` decrementa il contatore.

```csharp
public override bool IsPowering() => _ticksLeft > 0;
public override void ChangeState() => _ticksLeft = _pulseDuration;
public void Tick() { if (_ticksLeft > 0) _ticksLeft--; }
```

**`RedstoneBlock`** è la sorgente più semplice: `IsPowering()` restituisce sempre `true` e non ha stato interno modificabile.

**`Torch`** implementa `IPowerable` e `IConnectable`. Il campo `_on` è impostato nel costruttore a `true` (torcia accesa di default). `UpdatePower` implementa la logica di inversione: `_on = incomingPower.Value == 0`. `CanConnectTo` blocca il lato di montaggio per le torce a muro (`!_vertical && direction == _placement`). `CurrentPower` è una proprietà con setter privato perché `UpdatePower` deve modificarla.

**`Repeater`** implementa `IPowerable` e `IConnectable`. Il campo `_output` determina la direzione di uscita. `CanConnectTo` accetta solo il lato opposto all'uscita (lato di ingresso). `UpdatePower` imposta `_on` e aggiorna `CurrentPower` a 15 se il ripetitore riceve segnale, 0 altrimenti. `ChangeState()` cicla il ritardo da 1 a 4. Il costruttore accetta un secondo parametro opzionale `int delay = 1`, validato con `Math.Clamp(delay, 1, 4)`, per ripristinare il ritardo al caricamento dal JSON.

**`RedstoneLamp`** implementa solo `IPowerable`. È un nodo terminale: riceve segnale da `ResolveLamps()`, aggiorna `CurrentPower`, ma non propaga nulla. `IsPowered()` restituisce `true` se `CurrentPower.Value > 0`.

### 5.14 Value Objects — `Position` e `SignalStrength`

**`Position`** incapsula le coordinate X e Y di una cella. Il metodo `Move(Direction)` restituisce la posizione adiacente nella direzione indicata, applicando le convenzioni dell'enum `Direction` (North = Y-1, South = Y+1, West = X-1, East = X+1). Sovrascrive `Equals` e `GetHashCode` per essere usabile come chiave in un `HashSet<Position>` o `Dictionary`.

```csharp
public Position Move(Direction direction) => direction switch
{
    Direction.North => new Position(X, Y - 1),
    Direction.South => new Position(X, Y + 1),
    Direction.West  => new Position(X - 1, Y),
    Direction.East  => new Position(X + 1, Y),
    _               => new Position(X, Y)
};
```

**`SignalStrength`** incapsula la potenza del segnale nell'intervallo 0–15, usando `Math.Clamp` nel costruttore per garantire che valori fuori range vengano silenziosamente corretti. Le proprietà statiche `Min` e `Max` forniscono i valori di bordo senza richiedere istanziazioni ripetute.

### 5.15 `JsonCircuitRepository.SaveAsync()` e `LoadAsync()`

```csharp
public static async Task SaveAsync(CircuitBoard board, string path)
{
    var data = new CircuitData { Width = board.Width, Height = board.Height };
    foreach (var pos in board.AllPositions())
    {
        var cell = ToCellData(board.GetEntity(pos), pos);
        if (cell != null) data.Cells.Add(cell);
    }
    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, _options));
}
```

`SaveAsync` scorre la griglia tramite `AllPositions()`, converte ogni entità non-Air in un `CellData` tramite `ToCellData()` (che usa un pattern matching per estrarre le proprietà rilevanti), e serializza l'intero oggetto `CircuitData` su file con indentazione. Le celle Air vengono scartate (`ToCellData` restituisce `null` per Air), rendendo i file compatti.

```csharp
public static async Task<CircuitBoard> LoadAsync(string path)
{
    var json = await File.ReadAllTextAsync(path);
    var data = JsonSerializer.Deserialize<CircuitData>(json)
               ?? throw new InvalidDataException("Could not read circuit file.");
    var board = new CircuitBoard(data.Width, data.Height);
    foreach (var cell in data.Cells)
    {
        var entity = ToEntity(cell);
        if (entity != null) board.PlaceEntity(entity, new Position(cell.X, cell.Y));
    }
    return board;
}
```

`LoadAsync` esegue il percorso inverso: legge il file in modo asincrono, deserializza il JSON in `CircuitData`, crea un nuovo `CircuitBoard` e richiama `ToEntity(CellData)` per ricostruire ogni entità. I campi nullable del DTO vengono gestiti con `??` per valori di default sicuri. Lo stato della Lever (`IsOn`) e il ritardo del Repeater (`Delay`) vengono passati direttamente al costruttore, garantendo che il circuito ricaricato sia identico a quello salvato.

`ToCellData()` usa un `switch` expression per mappare ogni tipo di entità nel DTO, includendo solo i campi significativi:

```csharp
private static CellData? ToCellData(Entity entity, Position pos) => entity switch
{
    Wire          => new CellData { X = pos.X, Y = pos.Y, Type = "Wire" },
    Lever l       => new CellData { X = pos.X, Y = pos.Y, Type = "Lever", IsOn = l.IsOn },
    Torch t       => new CellData { X = pos.X, Y = pos.Y, Type = "Torch",
                         Direction = t.Placement.ToString(), Vertical = t.IsVertical },
    Repeater r    => new CellData { X = pos.X, Y = pos.Y, Type = "Repeater",
                         Direction = r.Output.ToString(), Delay = r.Delay },
    Button        => new CellData { X = pos.X, Y = pos.Y, Type = "Button" },
    RedstoneBlock => new CellData { X = pos.X, Y = pos.Y, Type = "RedstoneBlock" },
    RedstoneLamp  => new CellData { X = pos.X, Y = pos.Y, Type = "RedstoneLamp" },
    Block         => new CellData { X = pos.X, Y = pos.Y, Type = "Block" },
    _             => null
};
```

`ToEntity()` fa il percorso inverso, passando i valori salvati ai costruttori:

```csharp
private static Entity? ToEntity(CellData cell) => cell.Type switch
{
    "Wire"          => new Wire(),
    "Block"         => new Block(),
    "Lever"         => new Lever(cell.IsOn ?? false),
    "Torch"         => new Torch(cell.Vertical ?? true,
                           Enum.Parse<Direction>(cell.Direction ?? "North")),
    "Repeater"      => new Repeater(Enum.Parse<Direction>(cell.Direction ?? "North"),
                           cell.Delay ?? 1),
    "Button"        => new Button(),
    "RedstoneBlock" => new RedstoneBlock(),
    "RedstoneLamp"  => new RedstoneLamp(),
    _               => null
};
```

### 5.16 `MainWindowViewModel` — Il ViewModel Principale

Il `MainWindowViewModel` è la classe che connette la logica di simulazione all'interfaccia grafica. Viene istanziato una volta all'avvio e impostato come `DataContext` della finestra principale.

**Costruttore:**
```csharp
public MainWindowViewModel()
{
    _board     = new CircuitBoard(GridWidth, GridHeight);
    _simulator = new TickSimulator(_board);

    _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
    _timer.Tick += (_, _) => DoStep();

    for (int y = 0; y < GridHeight; y++)
        for (int x = 0; x < GridWidth; x++)
            Cells.Add(new CellViewModel(x, y, OnCellClicked));
}
```

Il costruttore crea la griglia, il simulatore, il timer Avalonia e la collezione di `CellViewModel`. Il timer è configurato a 200 ms (5 tick al secondo) e il suo handler chiama semplicemente `DoStep()`. Le celle sono create in ordine riga-prima (Y esterno, X interno) per corrispondere all'ordine in cui `UniformGrid` le dispone nell'interfaccia.

**Comandi di simulazione:**

`Play()` avvia il timer e imposta `IsRunning = true`. `Pause()` ferma il timer. `Step()` delega a `DoStep()`. `Reset()` ferma il timer, ricrea `CircuitBoard` e `TickSimulator` da zero, azzera `TickCount` e aggiorna ogni cella nella collezione impostando `BlockType = Air` prima di chiamare `Refresh()`.

**`SelectTool(string tool)`** usa `Enum.TryParse<BlockType>` per convertire il tag della barra degli strumenti in un valore enum, poi aggiorna `SelectedBlockType` e `StatusText`.

**`RotateDirection()`** fa avanzare `SelectedDirection` usando il modulo: `(Direction)(((int)SelectedDirection + 1) % 4)`, ciclando N → E → S → W → N.

### 5.17 `OnCellClicked()` — Gestione del Clic sulla Griglia

```csharp
private void OnCellClicked(CellViewModel cell)
{
    var pos      = new Position(cell.X, cell.Y);
    var existing = _board.GetEntity(pos);

    if (SelectedBlockType == BlockType.Air)
    {
        _board.RemoveEntity(pos);
    }
    else if (EntityToBlockType(existing) == SelectedBlockType)
    {
        existing.ChangeState();
    }
    else
    {
        _board.PlaceEntity(CreateEntity(SelectedBlockType, SelectedDirection), pos);
    }

    SyncCell(cell);
    DoStep();
}
```

Questo metodo implementa la logica a tre casi per il clic su una cella:
1. **Strumento Cancella** (`BlockType.Air`): rimuove l'entità sostituendola con Air.
2. **Stesso tipo già presente**: chiama `ChangeState()` sull'entità esistente — questo accende/spegne una leva, innesca un pulsante, oppure cicla il ritardo di un ripetitore.
3. **Tipo diverso**: crea una nuova entità con `CreateEntity(SelectedBlockType, SelectedDirection)` e la posiziona sulla griglia.

Dopo qualsiasi modifica, `SyncCell(cell)` aggiorna il ViewModel e `DoStep()` ricalcola immediatamente la simulazione per riflettere il cambiamento.

`CreateEntity()` è un factory method che usa un `switch` expression per istanziare il tipo corretto:

```csharp
private static Entity CreateEntity(BlockType type, Direction dir) => type switch
{
    BlockType.Wire          => new Wire(),
    BlockType.Lever         => new Lever(),
    BlockType.Button        => new Button(),
    BlockType.RedstoneBlock => new RedstoneBlock(),
    BlockType.RedstoneLamp  => new RedstoneLamp(),
    BlockType.Torch         => new Torch(true, dir),
    BlockType.Repeater      => new Repeater(dir),
    BlockType.Block         => new Block(),
    _                       => new Air()
};
```

### 5.18 `DoStep()` e `RefreshAll()` — Ciclo di Aggiornamento

```csharp
private void DoStep()
{
    _simulator.Tick();
    TickCount = _simulator.TickCount;
    RefreshAll();
}

private void RefreshAll()
{
    foreach (var c in Cells) SyncCell(c);
}
```

`DoStep()` esegue un tick del simulatore, sincronizza il contatore e aggiorna tutti i ViewModel. `RefreshAll()` scorre l'intera collezione `Cells` e chiama `SyncCell()` su ogni elemento. Questo approccio è semplice e corretto: per una griglia 20×15 = 300 celle il costo è trascurabile, e la semplicità del codice è prioritaria su un'ottimizzazione prematura.

### 5.19 `SyncCell()` — Sincronizzazione tra Dominio e ViewModel

```csharp
private void SyncCell(CellViewModel vm)
{
    var e = _board.GetEntity(new Position(vm.X, vm.Y));
    vm.BlockType  = EntityToBlockType(e);
    vm.IsOn       = e.IsPowering() || (e is RedstoneLamp l && l.IsPowered());
    vm.PowerLevel = e is IPowerable pw ? pw.CurrentPower.Value : 0;
    vm.Direction  = e is Repeater r ? r.Output
                  : e is Torch t    ? t.Placement
                  : Direction.North;
    vm.Refresh();
}
```

`SyncCell()` è il ponte tra lo stato del dominio e la view. Legge l'entità dal `CircuitBoard`, ne estrae le proprietà rilevanti e le copia nel ViewModel. La condizione su `IsOn` è deliberatamente composita: `IsPowering()` cattura le sorgenti (Lever, Button, RedstoneBlock, Torch accesa), mentre il caso speciale `RedstoneLamp` usa `IsPowered()` perché la lampada non "genera" potenza, ma la riceve.

`EntityToBlockType()` mappa il tipo runtime dell'entità all'enum `BlockType`, usato da `CellViewModel` per calcolare `Symbol` e `CellColor`.

Dopo aver aggiornato tutte le proprietà osservabili backing, `vm.Refresh()` notifica la view di rileggere le tre proprietà calcolate (`Symbol`, `CellColor`, `ForeColor`) in un'unica operazione.

### 5.20 `CellViewModel` — Proprietà Calcolate e Delegate Pattern

```csharp
public partial class CellViewModel : ViewModelBase
{
    public int X { get; }
    public int Y { get; }
    [ObservableProperty] private BlockType _blockType = BlockType.Air;
    [ObservableProperty] private Direction _direction = Direction.North;
    [ObservableProperty] private bool _isOn;
    [ObservableProperty] private int _powerLevel;

    private readonly Action<CellViewModel> _onClicked;
    public IRelayCommand ClickCommand { get; }

    public CellViewModel(int x, int y, Action<CellViewModel> onClicked)
    {
        X = x; Y = y;
        _onClicked = onClicked;
        ClickCommand = new RelayCommand(() => _onClicked(this));
    }
}
```

Il `CellViewModel` usa il source generator di CommunityToolkit.MVVM (`[ObservableProperty]`) per generare automaticamente le proprietà observable con le notifiche `INotifyPropertyChanged`. Questo riduce il boilerplate rispetto all'implementazione manuale.

Il `ClickCommand` è un `RelayCommand` che quando eseguito chiama il delegate `_onClicked(this)`, passando se stesso come argomento. Il delegate viene fornito nel costruttore da `MainWindowViewModel.OnCellClicked`. Questo pattern elimina la necessità di ancestor bindings nel XAML, che non sono supportati dai compiled bindings di Avalonia.

Le tre proprietà calcolate non sono proprietà observable standard ma vengono aggiornate tramite `Refresh()`:

```csharp
public string Symbol => BlockType switch
{
    BlockType.Lever         => IsOn  ? "│" : "╱",
    BlockType.Button        => IsOn  ? "●" : "○",
    BlockType.Torch         => IsOn  ? "↑" : "↓",
    BlockType.Repeater      => Direction switch
    {
        Direction.North => "▲", Direction.East  => "▶",
        Direction.South => "▼", Direction.West  => "◀", _ => "▶"
    },
    BlockType.RedstoneLamp  => IsOn  ? "✦" : "✧",
    BlockType.RedstoneBlock => "■",
    _                       => " "
};
```

`CellColor` usa un gradiente rosso per i fili calcolato da `WireRed(int power)`:

```csharp
private static string WireRed(int power)
{
    double t = power / 15.0;
    int r = (int)(40  + t * 215);  // da 40 (#28) a 255 (#FF)
    int g = (int)(0   + t * 60);   // tocco di arancione ai livelli alti
    return $"#{r:X2}{g:X2}00";
}
```

Il canale verde aumenta leggermente ai livelli di potenza più alti, producendo una sfumatura che vira dall'arancione scuro (`#2A0000`) al rosso-arancio brillante (`#FF3C00`), simile al comportamento visivo nel gioco originale.

`Refresh()` notifica manualmente il binding system per tutte e tre le proprietà calcolate in una singola chiamata, evitando tre chiamate separate che potrebbero innescare tre ridisegni della cella:

```csharp
public void Refresh()
{
    OnPropertyChanged(nameof(Symbol));
    OnPropertyChanged(nameof(CellColor));
    OnPropertyChanged(nameof(ForeColor));
}
```

### 5.21 Flusso di Interazione Completo

Per chiarire l'integrazione tra i livelli, ecco il flusso completo quando l'utente accende una leva:

1. L'utente clicca su una cella che contiene una `Lever`.
2. Il `Button` 36×36 nel AXAML esegue `ClickCommand`.
3. `CellViewModel.ClickCommand` chiama `_onClicked(this)`.
4. `MainWindowViewModel.OnCellClicked(cell)` viene invocato.
5. Dato che `EntityToBlockType(existing) == SelectedBlockType == Lever`, chiama `existing.ChangeState()`.
6. `Lever.ChangeState()` imposta `_on = !_on` → leva accesa.
7. `SyncCell(cell)` aggiorna `vm.IsOn = true` e chiama `vm.Refresh()`.
8. `DoStep()` chiama `_simulator.Tick()`.
9. `TickSimulator.Tick()`: `ResetAllPower()` azzera i fili → `CollectSources()` trova la leva accesa e accoda i suoi vicini a 15 → `BfsFlood()` propaga il segnale attraverso i fili → `ResolveTorches()`, `BfsFlood`, `ResolveRepeaters()`, `ResolveLamps()`.
10. `RefreshAll()` sincronizza tutti i ViewModel con lo stato aggiornato del dominio.
11. Avalonia aggiorna l'interfaccia grafica mostrando i fili colorati e la lampada accesa.
