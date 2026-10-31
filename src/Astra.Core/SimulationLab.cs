using System.Globalization;
namespace AstraOS.Core;

public sealed record SimulationParameter(string Id,string Label,double Default,double Min,double Max);
public sealed record SimulationTool(string Id,string Name,string Subjects,string Assumptions,string[] Fields,SimulationParameter[] Parameters);
public sealed record SimulationResult(ScientificScene Scene,ScientificDataset Data,string Summary);

/// <summary>Bounded, deterministic educational numerical models; all parameters have displayed units.</summary>
public static class SimulationLab
{
    private static SimulationParameter P(string id,string label,double value,double min,double max)=>new(id,label,value,min,max);
    public static IReadOnlyList<SimulationTool> Tools { get; } = new[]{
        new SimulationTool("orbit",AstraOS.Core.UiText.T("Meccanica orbitale · propagatore RK4"),AstraOS.Core.UiText.T("Astrodinamica · perturbazione J₂"),
            AstraOS.Core.UiText.T("Stato cartesiano ECI in km e s; gravità terrestre centrale con J₂ opzionale. Impulso iniziale tangenziale. Nessuna atmosfera, Luna o manovra. Passo fisso ≤ 10 s; arresto all'intersezione del suolo."),new[]{"aerospace","physics"},new[]{P("alt",AstraOS.Core.UiText.T("Quota iniziale (km)"),400,100,36000),P("speed",AstraOS.Core.UiText.T("Velocità tangenziale (km/s)"),7.668558,0,12),P("inc",AstraOS.Core.UiText.T("Inclinazione (°)"),51.6,0,180),P("duration",AstraOS.Core.UiText.T("Durata (s)"),5600,10,20000),P("j2",AstraOS.Core.UiText.T("Includi J₂ (0 oppure 1)"),1,0,1)}),
        new("gnc",AstraOS.Core.UiText.T("GNC · assetto quaternionico"),AstraOS.Core.UiText.T("Controllo di assetto · corpo rigido"),
            AstraOS.Core.UiText.T("Equazioni di Eulero e cinematica del quaternione, RK4 Δt=0,02 s. Controllore PD verso identità, coppia saturata per asse. Inerzie principali; assenti disturbi, rumore e stima del sensore."),new[]{"aerospace","automation","robotics"},new[]{P("angle",AstraOS.Core.UiText.T("Errore iniziale asse Y (°)"),35,-170,170),P("ix",AstraOS.Core.UiText.T("Inerzia Ix (kg m²)"),12,0.1,1000),P("iy",AstraOS.Core.UiText.T("Inerzia Iy (kg m²)"),15,0.1,1000),P("iz",AstraOS.Core.UiText.T("Inerzia Iz (kg m²)"),10,0.1,1000),P("kp",AstraOS.Core.UiText.T("Guadagno proporzionale (N m)"),1,0.01,10),P("kd",AstraOS.Core.UiText.T("Guadagno derivativo (N m s)"),3,0.1,10),P("torque",AstraOS.Core.UiText.T("Coppia massima / asse (N m)"),0.5,0.01,5)}),
        new("flight",AstraOS.Core.UiText.T("Meccanica del volo · traiettoria longitudinale"),AstraOS.Core.UiText.T("Dinamica puntiforme 3DOF · salita e planata"),
            AstraOS.Core.UiText.T("Terra piana, atmosfera ISA troposferica 0–11 km; massa costante, CL e spinta costanti; CD=CD₀+CL²/(πARe). RK4 0,05 s. Assenti vento, stallo, Mach e moto laterale; termina a V<20 m/s o al suolo."),new[]{"aerospace"},new[]{P("mass",AstraOS.Core.UiText.T("Massa (kg)"),1200,100,100000),P("area",AstraOS.Core.UiText.T("Superficie alare (m²)"),16,1,500),P("speed",AstraOS.Core.UiText.T("Velocità iniziale (m/s)"),65,25,200),P("alt",AstraOS.Core.UiText.T("Quota iniziale (m)"),1000,10,10000),P("cl",AstraOS.Core.UiText.T("Coefficiente CL"),0.45,-0.2,1),P("thrust",AstraOS.Core.UiText.T("Spinta (N)"),1500,0,100000),P("ar",AstraOS.Core.UiText.T("Allungamento AR"),8,2,25)}),
        new("aero",AstraOS.Core.UiText.T("Aerodinamica · polare di ala finita"),AstraOS.Core.UiText.T("Portanza · resistenza indotta · efficienza"),
            AstraOS.Core.UiText.T("Teoria lineare di ala finita: a=2π/(1+2/(e AR)), CL=aα, CD=CD₀+CL²/(πeAR). α da −8° a +8°, flusso incomprimibile attaccato; non risolve CFD, stallo o transonico."),new[]{"aerospace","mechanical","naval","transport"},new[]{P("ar",AstraOS.Core.UiText.T("Allungamento AR"),8,2,25),P("e",AstraOS.Core.UiText.T("Efficienza Oswald"),0.85,0.5,1),P("cd0",AstraOS.Core.UiText.T("CD parassita"),0.025,0.005,0.2),P("rho",AstraOS.Core.UiText.T("Densità (kg/m³)"),1.225,0.1,1025),P("speed",AstraOS.Core.UiText.T("Velocità (m/s)"),60,1,200),P("area",AstraOS.Core.UiText.T("Superficie (m²)"),16,0.1,1000)}),
        new("oscillator",AstraOS.Core.UiText.T("Meccanica · oscillatore smorzato"),AstraOS.Core.UiText.T("Vibrazioni · risposta libera"),
            AstraOS.Core.UiText.T("m x″+c x′+k x=0, RK4 con passo adeguato alla pulsazione. Sistema lineare a un grado di libertà, nessuna forzante. Posizione iniziale 1 m, velocità nulla."),new[]{"mechanical","civil","building","naval","materials","industrial","transport","biomedical","physics"},new[]{P("mass",AstraOS.Core.UiText.T("Massa (kg)"),1,0.1,1000),P("stiffness",AstraOS.Core.UiText.T("Rigidezza (N/m)"),20,0.1,1000),P("damping",AstraOS.Core.UiText.T("Smorzamento (N s/m)"),0.6,0,100),P("duration",AstraOS.Core.UiText.T("Durata (s)"),20,1,60)}),
        new("beam",AstraOS.Core.UiText.T("Strutture · trave di Euler–Bernoulli"),AstraOS.Core.UiText.T("Statica · flessione elastica"),
            AstraOS.Core.UiText.T("Trave a mensola con carico concentrato in estremità: y=P x²(3L−x)/(6EI). Materiale lineare, sezione costante, piccoli spostamenti; verifica indicativa δ/L, non dimensionamento normativo."),new[]{"civil","building","mechanical","naval","materials","geotechnical","industrial"},new[]{P("length",AstraOS.Core.UiText.T("Lunghezza (m)"),3,0.1,50),P("load",AstraOS.Core.UiText.T("Carico P (N)"),1000,0,100000),P("young",AstraOS.Core.UiText.T("Modulo E (GPa)"),210,0.1,1000),P("inertia",AstraOS.Core.UiText.T("Momento I (m⁴)"),8e-6,1e-9,1)}),
        new("rlc",AstraOS.Core.UiText.T("Circuiti · risposta RLC serie"),AstraOS.Core.UiText.T("Transitorio elettrico · smorzamento"),
            AstraOS.Core.UiText.T("Lq″+Rq′+q/C=V, gradino ideale, stato iniziale nullo. RK4 con Δt≤1/(100ω₀) e vincolo resistivo. Componenti lineari ideali, non include parassiti o switching."),new[]{"electrical","electronics","automation","robotics","computer","telecom","energy","biomedical","physics"},new[]{P("r",AstraOS.Core.UiText.T("Resistenza (Ω)"),10,0.1,100),P("l",AstraOS.Core.UiText.T("Induttanza (mH)"),100,10,1000),P("c",AstraOS.Core.UiText.T("Capacità (µF)"),100,10,1000),P("v",AstraOS.Core.UiText.T("Gradino (V)"),12,0,100)}),
        new("heat",AstraOS.Core.UiText.T("Termica · diffusione 1D"),AstraOS.Core.UiText.T("Conduzione · condizioni al contorno"),
            AstraOS.Core.UiText.T("∂T/∂t=α∂²T/∂x², differenze finite FTCS con αΔt/Δx²≤0,45. Barra omogenea 0°C, estremità sinistra 100°C, destra 0°C; 51 nodi, nessuna convezione o sorgente."),new[]{"energy","nuclear","chemical","materials","mechanical","electronics","environmental","geotechnical","agricultural","sciences","physics"},new[]{P("alpha",AstraOS.Core.UiText.T("Diffusività (m²/s)"),1e-4,1e-7,0.01),P("length",AstraOS.Core.UiText.T("Lunghezza (m)"),1,0.01,10),P("duration",AstraOS.Core.UiText.T("Durata (s)"),300,1,10000)}),
        new("reactor",AstraOS.Core.UiText.T("Processi · reattore batch isotermo"),AstraOS.Core.UiText.T("Cinetica chimica · A → B"),
            AstraOS.Core.UiText.T("Reazione irreversibile del primo ordine, C_A=C_A0 exp(−kt), C_B=C_A0−C_A. Miscelazione e volume ideali, temperatura costante; non include reazioni multiple o trasporto."),new[]{"chemical","environmental","biomedical","agricultural","sciences"},new[]{P("k",AstraOS.Core.UiText.T("Costante cinetica (s⁻¹)"),0.05,0.0001,10),P("initial",AstraOS.Core.UiText.T("Concentrazione iniziale (mol/m³)"),100,0.1,10000)}),
        new("signal",AstraOS.Core.UiText.T("Segnali · analizzatore DFT"),AstraOS.Core.UiText.T("Campionamento · spettro · aliasing"),
            AstraOS.Core.UiText.T("Somma di due sinusoidi, finestra Hann, DFT diretta su 512 campioni. Ampiezze corrette per guadagno coerente, assi in Hz e unità di ingresso; risoluzione Fs/512. Frequenze sopra Nyquist mostrano aliasing."),new[]{"telecom","electronics","electrical","computer","robotics","automation","biomedical","physics","mathematics"},new[]{P("fs",AstraOS.Core.UiText.T("Frequenza di campionamento (Hz)"),1000,10,100000),P("f1",AstraOS.Core.UiText.T("Frequenza 1 (Hz)"),50,0,100000),P("f2",AstraOS.Core.UiText.T("Frequenza 2 (Hz)"),120,0,100000),P("a2",AstraOS.Core.UiText.T("Ampiezza 2"),0.5,0,5)}),
        new("queue",AstraOS.Core.UiText.T("Sistemi · coda M/M/1"),AstraOS.Core.UiText.T("Ricerca operativa · capacità e attese"),
            AstraOS.Core.UiText.T("Soluzione stazionaria analitica: arrivi Poisson, servizio esponenziale, un servente FIFO, capacità infinita. Richiede λ<μ; non modella reti, priorità o traffico reale."),new[]{"management","industrial","computer","transport","telecom","mathematics"},new[]{P("lambda",AstraOS.Core.UiText.T("Arrivi λ (utenti/ora)"),8,0.01,10000),P("mu",AstraOS.Core.UiText.T("Servizio μ (utenti/ora)"),10,0.02,20000)}),
        new("lorenz",AstraOS.Core.UiText.T("Matematica · sistema di Lorenz"),AstraOS.Core.UiText.T("Sistemi dinamici · caos deterministico"),
            AstraOS.Core.UiText.T("dx/dt=σ(y−x), dy/dt=x(ρ−z)−y, dz/dt=xy−βz; RK4 Δt=0,005; (1,1,1) iniziale. Variabili adimensionali, modello dinamico idealizzato, non previsione meteorologica."),new[]{"mathematics","physics"},new[]{P("sigma","σ",10,1,30),P("rho","ρ",28,1,60),P("beta","β",2.6666667,0.1,10)}),
        new("ecology",AstraOS.Core.UiText.T("Scienze · dinamica predatore–preda"),AstraOS.Core.UiText.T("Biologia · equilibri non lineari"),
            AstraOS.Core.UiText.T("Lotka–Volterra, RK4 Δt=0,01. Popolazioni continue in unità convenzionali, ambiente chiuso, parametri costanti; non include capacità portante, stagionalità o dati osservati."),new[]{"sciences","environmental","agricultural","mathematics"},new[]{P("alpha",AstraOS.Core.UiText.T("Tasso crescita prede (1/tempo)"),1.1,0.1,3),P("beta",AstraOS.Core.UiText.T("Predazione (1/popolazione/tempo)"),0.4,0.05,1),P("delta",AstraOS.Core.UiText.T("Conversione predatori"),0.1,0.01,0.5),P("gamma",AstraOS.Core.UiText.T("Mortalità predatori (1/tempo)"),0.4,0.1,2)})
    };
    public static IReadOnlyList<SimulationTool> For(string domain,string track)=>Tools.Where(t=>domain=="general"||domain=="engineering"&&track=="other"||t.Fields.Contains(StudyCatalog.Key(domain,track))).ToArray();
    public static SimulationResult Run(string id,IReadOnlyDictionary<string,double>? input=null,CancellationToken cancel=default)
    {
        var tool=Tools.Single(t=>t.Id==id);var p=tool.Parameters.ToDictionary(x=>x.Id,x=>input is not null&&input.TryGetValue(x.Id,out var v)?v:x.Default);
        foreach(var v in tool.Parameters)if(!double.IsFinite(p[v.Id])||p[v.Id]<v.Min||p[v.Id]>v.Max)throw new ArgumentException(v.Label+AstraOS.Core.UiText.T(": valore fuori intervallo."));
        double V(string k)=>p[k];var rows=new List<double?[]>();var series=new List<SceneSeries>();string[] cols=Array.Empty<string>();string summary="";string ax="X",ay="Y",az="Z";
        void Row(params double[] values){if(values.Any(v=>!double.IsFinite(v)||Math.Abs(v)>1e100))throw new InvalidDataException(AstraOS.Core.UiText.T("Integrazione divergente: modifica parametri o orizzonte."));rows.Add(values.Select(v=>(double?)v).ToArray());}
        void Line(string name,IEnumerable<ScenePoint> points)=>series.Add(new(name,"line",points.ToArray()));
        void TimeLine(int y,string name)=>Line(name,rows.Select(r=>new ScenePoint(r[0]!.Value,r[y]!.Value,0)));
        switch(id)
        {
            case "orbit":
            {
                if(V("j2") is not (0 or 1))throw new ArgumentException(AstraOS.Core.UiText.T("J₂ deve essere 0 oppure 1."));
                const double mu=398600.4418,re=6378.137,j2=1.08262668e-3;double r=re+V("alt"),inc=V("inc")*Math.PI/180;
                double[] s={r,0,0,0,V("speed")*Math.Cos(inc),V("speed")*Math.Sin(inc)};cols=new[]{"t_s","x_km","y_km","z_km","vx_km_s","vy_km_s","vz_km_s","energy_km2_s2"};double initial=0,last=0;bool impact=false;
                double[] F(double[] q){double rr=Math.Sqrt(q[0]*q[0]+q[1]*q[1]+q[2]*q[2]),g=-mu/(rr*rr*rr),f=1.5*j2*mu*re*re/Math.Pow(rr,5)*Math.Round(V("j2")),z2=q[2]*q[2]/(rr*rr);return new[]{q[3],q[4],q[5],g*q[0]+f*q[0]*(5*z2-1),g*q[1]+f*q[1]*(5*z2-1),g*q[2]+f*q[2]*(5*z2-3)};}
                int n=(int)Math.Ceiling(V("duration")/10);double dt=V("duration")/n;
                for(int i=0;i<=n;i++){cancel.ThrowIfCancellationRequested();double rr=Math.Sqrt(s[0]*s[0]+s[1]*s[1]+s[2]*s[2]);last=(s[3]*s[3]+s[4]*s[4]+s[5]*s[5])/2-mu/rr-(Math.Round(V("j2"))==1?mu*j2*re*re/(2*rr*rr*rr)*(1-3*s[2]*s[2]/(rr*rr)):0);if(i==0)initial=last;Row(i*dt,s[0],s[1],s[2],s[3],s[4],s[5],last);if(rr<re){impact=true;break;}s=Rk4(s,dt,F);}
                Line("Traiettoria ECI",rows.Select(q=>new ScenePoint(q[1]!.Value,q[2]!.Value,q[3]!.Value)));ax="X ECI (km)";ay="Y ECI (km)";az="Z ECI (km)";summary=AstraOS.Core.UiText.F("{0} stati · Δt={1:G4} s · deriva relativa energia totale {2:G4}", rows.Count, dt, Math.Abs((last-initial)/initial))+(impact?AstraOS.Core.UiText.T(" · ARRESTO: intersezione superficie terrestre"):"");break;
            }
            case "gnc":
            {
                double a=V("angle")*Math.PI/360;double[] s={Math.Cos(a),0,Math.Sin(a),0,0,0,0};cols=new[]{"t_s","error_deg","wx_rad_s","wy_rad_s","wz_rad_s","q_norm","qw","qx","qy","qz"};double[] inertia={V("ix"),V("iy"),V("iz")};
                double[] F(double[] q){double w=q[0],x=q[1],y=q[2],z=q[3],wx=q[4],wy=q[5],wz=q[6],sgn=w<0?-1:1;double[] t=Enumerable.Range(0,3).Select(k=>Math.Clamp(-V("kp")*sgn*q[k+1]-V("kd")*q[k+4],-V("torque"),V("torque"))).ToArray();return new[]{-.5*(x*wx+y*wy+z*wz),.5*(w*wx+y*wz-z*wy),.5*(w*wy+z*wx-x*wz),.5*(w*wz+x*wy-y*wx),(t[0]-(inertia[2]-inertia[1])*wy*wz)/inertia[0],(t[1]-(inertia[0]-inertia[2])*wz*wx)/inertia[1],(t[2]-(inertia[1]-inertia[0])*wx*wy)/inertia[2]};}
                var axis=new List<ScenePoint>();for(int i=0;i<=6000;i++){cancel.ThrowIfCancellationRequested();double norm=Math.Sqrt(s.Take(4).Sum(x=>x*x));for(int k=0;k<4;k++)s[k]/=norm;Row(i*.02,2*Math.Acos(Math.Clamp(Math.Abs(s[0]),0,1))*180/Math.PI,s[4],s[5],s[6],norm,s[0],s[1],s[2],s[3]);axis.Add(new(1-2*(s[2]*s[2]+s[3]*s[3]),2*(s[1]*s[2]+s[0]*s[3]),2*(s[1]*s[3]-s[0]*s[2])));s=Rk4(s,.02,F);}
                Line("Asse X corpo nello spazio inerziale",axis);summary=AstraOS.Core.UiText.F("Errore finale {0:G5}° · 120 s · coppia limitata ±{1:G4} N m", rows[^1][1], V("torque"));ax="X asse corpo";ay="Y asse corpo";az="Z asse corpo";break;
            }
            case "flight":
            {
                double[] s={0,V("alt"),V("speed"),0};cols=new[]{"t_s","range_m","height_m","speed_m_s","gamma_deg","lift_N","drag_N"};
                (double L,double D) Forces(double[] q){double t=288.15-.0065*Math.Clamp(q[1],0,11000),rho=1.225*Math.Pow(t/288.15,4.25588),dyn=.5*rho*q[2]*q[2]*V("area");return(dyn*V("cl"),dyn*(.025+V("cl")*V("cl")/(Math.PI*V("ar")*.85)));}
                double[] F(double[] q){var f=Forces(q);double speed=Math.Max(20,q[2]);return new[]{speed*Math.Cos(q[3]),speed*Math.Sin(q[3]),(V("thrust")-f.D)/V("mass")-9.80665*Math.Sin(q[3]),f.L/(V("mass")*speed)-9.80665*Math.Cos(q[3])/speed};}
                for(int i=0;i<=2400;i++){cancel.ThrowIfCancellationRequested();var f=Forces(s);Row(i*.05,s[0],s[1],s[2],s[3]*180/Math.PI,f.L,f.D);if(s[1]<=0||s[1]>11000||s[2]<20||s[2]>250)break;s=Rk4(s,.05,F);}Line("Traiettoria",rows.Select(q=>new ScenePoint(q[1]!.Value,0,q[2]!.Value)));ax="Distanza (m)";ay="Laterale (m)";az="Quota (m)";summary=AstraOS.Core.UiText.F("Termine a t={0:F2} s · quota {1:F1} m · V={2:F2} m/s. Controlla eventuale uscita dal dominio del modello.", rows[^1][0], rows[^1][2], rows[^1][3]);break;
            }
            case "aero":
                cols=new[]{"alpha_deg","CL","CD","lift_N","drag_N","L_D"};double slope=2*Math.PI/(1+2/(V("e")*V("ar")));for(int i=0;i<=160;i++){double alpha=-8+i*.1,cl=slope*alpha*Math.PI/180,cd=V("cd0")+cl*cl/(Math.PI*V("e")*V("ar")),q=.5*V("rho")*V("speed")*V("speed")*V("area");Row(alpha,cl,cd,q*cl,q*cd,cl/cd);}Line("Polare CD–CL",rows.Select(q=>new ScenePoint(q[2]!.Value,q[1]!.Value,0)));ax="CD";ay="CL";summary=AstraOS.Core.UiText.F("Pendenza CLα={0:F4} rad⁻¹ · efficienza massima campionata {1:F3}", slope, rows.Max(r=>r[5]));break;
            case "oscillator":
            {
                double m=V("mass"),k=V("stiffness"),c=V("damping"),wn=Math.Sqrt(k/m),dt=Math.Min(.02,Math.Min(1/(100*wn),c>0?m/(20*c):.02));int n=(int)Math.Ceiling(V("duration")/dt);if(n>100000)throw new ArgumentException(AstraOS.Core.UiText.T("Combinazione troppo rigida: riduci durata o smorzamento."));dt=V("duration")/n;double[] s={1,0};cols=new[]{"t_s","displacement_m","velocity_m_s","energy_J"};for(int i=0;i<=n;i++){cancel.ThrowIfCancellationRequested();if(i%Math.Max(1,(int)Math.Ceiling(n/6000.0))==0||i==n)Row(i*dt,s[0],s[1],.5*m*s[1]*s[1]+.5*k*s[0]*s[0]);s=Rk4(s,dt,q=>new[]{q[1],-(c*q[1]+k*q[0])/m});}TimeLine(1,"Spostamento");ax="Tempo (s)";ay="Spostamento (m)";summary=AstraOS.Core.UiText.F("ωₙ={0:G5} rad/s · ζ={1:G5} · Δt={2:G4} s", wn, c/(2*Math.Sqrt(m*k)), dt);break;
            }
            case "beam":
                cols=new[]{"x_m","deflection_m","moment_Nm"};for(int i=0;i<=200;i++){double x=V("length")*i/200;Row(x,V("load")*x*x*(3*V("length")-x)/(6*V("young")*1e9*V("inertia")),V("load")*(V("length")-x));}TimeLine(1,"Deformata");ax="Ascissa (m)";ay="Deflessione (m)";summary=AstraOS.Core.UiText.F("δ punta={0:G6} m · δ/L={1:G4}", rows[^1][1], rows[^1][1]/V("length"))+(rows[^1][1]/V("length")>.05?AstraOS.Core.UiText.T(" · IPOTESI PICCOLI SPOSTAMENTI NON VALIDA"):"");break;
            case "rlc":
            {
                double r=V("r"),l=V("l")/1000,c=V("c")/1e6,w=1/Math.Sqrt(l*c),duration=Math.Max(20/w,Math.Min(1,10*2*l/r)),dt=Math.Min(1/(100*w),l/(20*r));int n=(int)Math.Ceiling(duration/dt);if(n>100000)throw new ArgumentException(AstraOS.Core.UiText.T("Combinazione troppo rigida."));dt=duration/n;double[] s={0,0};cols=new[]{"t_s","voltage_C_V","current_A"};for(int i=0;i<=n;i++){cancel.ThrowIfCancellationRequested();if(i%Math.Max(1,(int)Math.Ceiling(n/6000.0))==0||i==n)Row(i*dt,s[0]/c,s[1]);s=Rk4(s,dt,q=>new[]{q[1],(V("v")-r*q[1]-q[0]/c)/l});}TimeLine(1,"Tensione condensatore");ax="Tempo (s)";ay="Tensione (V)";summary=AstraOS.Core.UiText.F("f₀={0:G5} Hz · ζ={1:G5} · Δt={2:G4} s", w/(2*Math.PI), r/2*Math.Sqrt(c/l), dt);break;
            }
            case "heat":
            {
                double dx=V("length")/50,dt=.45*dx*dx/V("alpha");int n=(int)Math.Ceiling(V("duration")/dt);if(n>500000)throw new ArgumentException(AstraOS.Core.UiText.T("Troppi passi termici: riduci durata/diffusività o aumenta lunghezza."));dt=V("duration")/n;double f=V("alpha")*dt/(dx*dx);var temp=new double[51];temp[0]=100;var surface=new List<ScenePoint>();cols=new[]{"t_s","x_m","temperature_C"};int stride=Math.Max(1,(int)Math.Ceiling(n/100.0));int saved=0;for(int i=0;i<=n;i++){cancel.ThrowIfCancellationRequested();if(i%stride==0||i==n){saved++;for(int j=0;j<=50;j++){Row(i*dt,j*dx,temp[j]);surface.Add(new(j*dx,i*dt,temp[j]));}}var next=(double[])temp.Clone();for(int j=1;j<50;j++)next[j]=temp[j]+f*(temp[j-1]-2*temp[j]+temp[j+1]);temp=next;}series.Add(new("Evoluzione termica","surface",surface,saved,51));ax="Posizione (m)";ay="Tempo (s)";az="Temperatura (°C)";summary=AstraOS.Core.UiText.F("51 nodi · {0} passi · numero di Fourier {1:F4} ≤ 0,45", n, f);break;
            }
            case "reactor":
                cols=new[]{"t_s","CA_mol_m3","CB_mol_m3"};for(int i=0;i<=500;i++){double t=8/V("k")*i/500,ca=V("initial")*Math.Exp(-V("k")*t);Row(t,ca,V("initial")-ca);}TimeLine(1,"Reagente A");TimeLine(2,"Prodotto B");ax="Tempo (s)";ay="Concentrazione (mol/m³)";summary=AstraOS.Core.UiText.F("Tempo di dimezzamento={0:G6} s · conservazione A+B={1:G6} mol/m³", Math.Log(2)/V("k"), V("initial"));break;
            case "signal":
            {
                const int n=512;var samples=Enumerable.Range(0,n).Select(i=>Math.Sin(2*Math.PI*V("f1")*i/V("fs"))+V("a2")*Math.Sin(2*Math.PI*V("f2")*i/V("fs"))).ToArray();var window=Enumerable.Range(0,n).Select(i=>.5-.5*Math.Cos(2*Math.PI*i/(n-1))).ToArray();double gain=window.Sum();cols=new[]{"frequency_Hz","amplitude"};for(int k=0;k<=n/2;k++){cancel.ThrowIfCancellationRequested();double re=0,im=0;for(int i=0;i<n;i++){double a=2*Math.PI*k*i/n;re+=samples[i]*window[i]*Math.Cos(a);im-=samples[i]*window[i]*Math.Sin(a);}Row(k*V("fs")/n,Math.Sqrt(re*re+im*im)/gain*(k==0||k==n/2?1:2));}TimeLine(1,"Spettro Hann");ax="Frequenza (Hz)";ay="Ampiezza";summary=AstraOS.Core.UiText.F("Δf={0:G5} Hz · Nyquist {1:G5} Hz", V("fs")/n, V("fs")/2)+((V("f1")>V("fs")/2||V("f2")>V("fs")/2)?AstraOS.Core.UiText.T(" · ALIASING: frequenza oltre Nyquist"):"");break;
            }
            case "queue":
                if(V("lambda")>=V("mu"))throw new ArgumentException(AstraOS.Core.UiText.T("Sistema instabile: λ deve essere minore di μ."));double rho=V("lambda")/V("mu");cols=new[]{"users_n","probability"};for(int i=0;i<=200;i++)Row(i,(1-rho)*Math.Pow(rho,i));TimeLine(1,"Distribuzione stazionaria");ax="Utenti";ay="Probabilità";summary=AstraOS.Core.UiText.F("ρ={0:F4} · L={1:G5} utenti · W={2:G5} min · Wq={3:G5} min", rho, rho/(1-rho), 60/(V("mu")-V("lambda")), 60*rho/(V("mu")-V("lambda")));break;
            case "lorenz":
            {
                double[] s={1,1,1};cols=new[]{"t","x","y","z"};for(int i=0;i<=8000;i++){cancel.ThrowIfCancellationRequested();Row(i*.005,s[0],s[1],s[2]);s=Rk4(s,.005,q=>new[]{V("sigma")*(q[1]-q[0]),q[0]*(V("rho")-q[2])-q[1],q[0]*q[1]-V("beta")*q[2]});}Line("Orbita nello spazio delle fasi",rows.Select(r=>new ScenePoint(r[1]!.Value,r[2]!.Value,r[3]!.Value)));summary=AstraOS.Core.UiText.T("8.001 stati · orizzonte 40 · Δt=0,005 · sensibilità alle condizioni iniziali");break;
            }
            case "ecology":
            {
                double[] s={10,5};cols=new[]{"time","prey","predators"};for(int i=0;i<=4000;i++){cancel.ThrowIfCancellationRequested();Row(i*.01,s[0],s[1]);s=Rk4(s,.01,q=>new[]{V("alpha")*q[0]-V("beta")*q[0]*q[1],V("delta")*q[0]*q[1]-V("gamma")*q[1]});}TimeLine(1,"Prede");TimeLine(2,"Predatori");ax="Tempo convenzionale";ay="Popolazione continua";summary=AstraOS.Core.UiText.F("Equilibrio positivo: prede={0:G4}, predatori={1:G4}", V("gamma")/V("delta"), V("alpha")/V("beta"));break;
            }
            default:throw new ArgumentException(AstraOS.Core.UiText.T("Strumento sconosciuto."));
        }
        if(rows.Count>10000||series.Sum(s=>s.Points.Count)>ScientificScene.MaxPoints)throw new InvalidDataException(AstraOS.Core.UiText.T("Risultato oltre i limiti del visualizzatore."));
        return new(new(tool.Name,ax,ay,az,series),new(tool.Name,cols,rows),summary);
    }
    private static double[] Rk4(double[] s,double dt,Func<double[],double[]> f)
    {
        var a=f(s);double[] Add(double[] d,double factor)=>s.Select((v,i)=>v+dt*factor*d[i]).ToArray();var b=f(Add(a,.5));var c=f(Add(b,.5));var d=f(Add(c,1));return s.Select((v,i)=>v+dt/6*(a[i]+2*b[i]+2*c[i]+d[i])).ToArray();
    }
}
