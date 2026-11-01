# Ipotesi, costanti e riferimento

- Quota iniziale: h=400 km sopra una superficie sferica di raggio R=6378.137 km; r=6778.137 km.
- Parametro gravitazionale adottato per il benchmark: μ=398600.4418 km³/s², costante nel tempo.
- Coordinate cartesiane ECI, origine al centro terrestre e assi inerziali ideali. L'orbita iniziale è nel piano XY, con r=(r,0,0) km e v=(0,√(μ/r),0) km/s.
- Unità coerenti: km per lunghezze, s per tempi, km/s per velocità, km/s² per accelerazione, km²/s² per energia specifica. Non mescolare metri con chilometri.
- Corpo puntiforme di massa trascurabile; massa e geometria CubeSat non influenzano questo modello a due corpi. Nessuna prestazione di un veicolo reale viene inferita.
- Terra sferica con gravità centrale; atmosfera, attrito/drag, J2, Sole/Luna, pressione di radiazione, manovre e incertezze sono esclusi.
- RK4 a passo nominale 30 s e 15 s, ultimo passo accorciato allo stesso istante finale. Durata predefinita: un periodo analitico. L'energia viene ricalcolata su tutti i campioni; la fase viene verificata contro la soluzione analitica circolare.

## Estensione futura J2 · UNVERIFIED_NOT_IMPLEMENTED

J2 non è attivo nel codice. Proposta da verificare: con z asse polare, q=z²/|r|² e f=(3/2)J2 μ R²/|r|⁵, aggiungere all'accelerazione centrale il vettore (fx(5q−1), fy(5q−1), fz(5q−3)). Un valore convenzionale candidato è J2=1.08262668e-3; richiede una fonte scientifica appropriata e test indipendenti prima dell'uso.

Verificare segno, unità, asse polare, potenziale coerente e precessione nodale su casi inclinati. L'energia specifica solo a due corpi non è l'invariante corretto per una simulazione con J2. La deriva nodale non viene misurata o rivendicata qui.

## Fonti e validità

Fonte primaria delle costanti adottate: [NGA, World Geodetic System 1984](https://earth-info.nga.mil/index.php?action=wgs84&dir=wgs84). Il semiasse equatoriale 6378137 m viene convertito in 6378.137 km e GM=3.986004418×10¹⁴ m³/s² in 398600.4418 km³/s². Qui si usa quel raggio come superficie sferica convenzionale del benchmark; non si modella l'ellissoide o la Terra reale completa. Questa è una fonte esterna esplicita, non una nota recuperata dal vault.

Queste sono costanti e assunzioni esplicite del benchmark, non osservazioni di volo. [fonti_vault.md](fonti_vault.md) registra soltanto materiale realmente recuperato da ASTRA. Se mancante o non pertinente, resta UNVERIFIED. Il controllo automatico conferma coerenza numerica interna di questo problema, non accuratezza di un modello di missione.
