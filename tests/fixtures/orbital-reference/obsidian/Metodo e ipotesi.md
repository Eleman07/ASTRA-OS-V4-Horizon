# Metodo e ipotesi

[[Progetto CubeSat 400 km]] · [[Risultati e verifiche]]

Quota 400 km, R=6378.137 km, μ=398600.4418 km³/s². Stato ECI in km e s; orbita iniziale circolare nel piano XY. dr/dt=v, dv/dt=−μr/|r|³; RK4 con passi 30 e 15 s e stesso tempo finale. Verifica distinta di energia, errore analitico e convergenza.

[Ipotesi complete e J2](../assumptions.md) · [architettura](../architecture.md) · [fonti realmente recuperate](../fonti_vault.md). Atmosfera, manovre e J2 non sono implementati. Nessuna validazione di volo.
