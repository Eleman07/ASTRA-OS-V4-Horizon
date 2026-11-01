namespace AstraOS.Core;

public sealed record DesktopCapability(string Name,string Category,string Mode,string Target,string Description);
public static class DesktopCapabilities
{
    public static readonly DesktopCapability[] All=
    {
        new("File, cartelle e ricerca","File","route","files","Copia, sposta, rinomina, anteprima, ZIP e Cestino."),
        new("App e launcher","Applicazioni","route","catalog","Applicazioni installate e ricerca del catalogo."),
        new("Finestre e Mission Control","Produttività","route","multitasking","Affiancamento, ripristino layout e sessioni di progetto."),
        new("Dock e accesso rapido","Produttività","route","home","Finestre aperte, icone reali e ripristino delle app."),
        new("Spotlight / ricerca rapida","Produttività","route","search","Ctrl+K cerca pagine e strumenti ASTRA."),
        new("Widget e automazioni","Produttività","route","widgets","Timer, note, attività e automazioni locali."),
        new("Memoria e assistente","Produttività","route","brain","Voce, documenti, web, studio e routine."),
        new("Terminale Ubuntu / WSL","Sviluppo","route","ubuntu","Distribuzioni Linux installate e terminale separato."),
        new("Plugin e strumenti sviluppo","Sviluppo","route","plugins","Plugin dichiarativi e SDK Python 3D."),
        new("Monitor attività","Sistema","route","diagnostics","Utilizzo reale di CPU, RAM e GPU disponibile."),
        new("Tema, puntatore e desktop","Aspetto","route","theme","Palette, sfondi, animazioni e leggibilità."),
        new("Display e monitor","Sistema","settings","ms-settings:display","Risoluzione, scala e monitor tramite Windows."),
        new("Audio e dispositivi","Sistema","settings","ms-settings:sound","Uscite, ingressi e volume tramite Windows."),
        new("Wi-Fi","Connessioni","settings","ms-settings:network-wifi","Reti wireless disponibili sul dispositivo."),
        new("Ethernet e rete","Connessioni","settings","ms-settings:network-status","Connessioni e stato della rete."),
        new("Bluetooth","Connessioni","settings","ms-settings:bluetooth","Abbinamento e gestione dei dispositivi."),
        new("VPN e proxy","Connessioni","settings","ms-settings:network-vpn","Connessioni VPN tramite Windows."),
        new("Stampanti e scanner","Dispositivi","settings","ms-settings:printers","Periferiche e stampa di sistema."),
        new("Mouse e touchpad","Dispositivi","settings","ms-settings:mousetouchpad","Preferenze delle periferiche di puntamento."),
        new("Tastiera e lingue","Dispositivi","settings","ms-settings:regionlanguage","Lingua Windows, layout e lingue installate."),
        new("Energia e sospensione","Sistema","settings","ms-settings:powersleep","Consumi, sospensione e profili energetici."),
        new("Archiviazione e dischi","Sistema","settings","ms-settings:storagesense","Spazio disponibile e gestione archiviazione."),
        new("Notifiche e concentrazione","Produttività","settings","ms-settings:notifications","Notifiche gestite dal sistema operativo."),
        new("Desktop virtuali e Snap","Produttività","settings","ms-settings:multitasking","Funzioni native Windows; sessioni ASTRA nel Multitasking."),
        new("App predefinite","Applicazioni","settings","ms-settings:defaultapps","Associazioni di file e browser."),
        new("App all'avvio","Applicazioni","settings","ms-settings:startupapps","Gestione delle applicazioni di avvio."),
        new("Aggiornamenti Windows","Sistema","settings","ms-settings:windowsupdate","Aggiornamenti tramite il sistema originale."),
        new("Account Windows","Sistema","settings","ms-settings:yourinfo","Account utente e informazioni del profilo."),
        new("Data e ora","Sistema","settings","ms-settings:dateandtime","Ora, calendario e fuso del sistema."),
        new("Backup Windows","Sistema","settings","ms-settings:backup","Backup e opzioni disponibili nella versione Windows."),
        new("Accessibilità visiva","Accessibilità","settings","ms-settings:easeofaccess-display","Testo, contrasto e dimensioni."),
        new("Narratore","Accessibilità","settings","ms-settings:easeofaccess-narrator","Lettura dello schermo tramite Windows."),
        new("Lente di ingrandimento","Accessibilità","settings","ms-settings:easeofaccess-magnifier","Ingrandimento del desktop."),
        new("Sottotitoli","Accessibilità","settings","ms-settings:easeofaccess-closedcaptioning","Preferenze dei sottotitoli di sistema."),
        new("Appunti Windows","Produttività","settings","ms-settings:clipboard","Cronologia Windows; scaffale manuale ASTRA qui sotto."),
        new("Modalità Gioco","Gaming","settings","ms-settings:gaming-gamemode","Impostazioni di gioco native Windows."),
        new("Catture e Game Bar","Gaming","settings","ms-settings:gaming-gamedvr","Registrazioni gestite dal sistema su richiesta."),
        new("Prestazioni grafiche app","Gaming","settings","ms-settings:display-advancedgraphics","Preferenze GPU per applicazione."),
        new("Luce notturna","Aspetto","settings","ms-settings:nightlight","Preferenze dello schermo tramite Windows."),
        new("Protezione del dispositivo","Sistema","route","security","Stato e accesso agli strumenti di sicurezza originali.")
    };
}
