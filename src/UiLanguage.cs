using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

// Window-local translations: no background work or per-frame overlay bindings.
internal static class UiLanguage {
    internal static string Normalize(string language){return language=="es"?"es":"en";}
    internal static string Read(Dictionary<string,object> settings){object value;return settings.TryGetValue("language",out value)?Normalize(Convert.ToString(value)):"en";}
    static readonly Dictionary<string,string> Spanish=new Dictionary<string,string>{
        {"◉   DPS Meter","◉   Medidor de DPS"},{"DPS Meter","Medidor de DPS"},{"Combat damage · experimental alpha","Daño en combate · versión alpha experimental"},
        {"Enable DPS meter","Activar medidor de DPS"},{"Self","Solo tú"},{"Party","Grupo"},{"Nearby players","Jugadores cercanos"},{"Show players","Mostrar jugadores"},
        {"Combat view","Vista de combate"},{"Healing","Curación"},{"Healing done","Curación realizada"},{"Healing received","Curación recibida"},
        {"Yes","Sí"},{"No","No"},{"All players","Todos los jugadores"},{"Filter by player","Filtrar por jugador"},{"Exports the selected fight using the player filter.","Exporta el combate seleccionado aplicando el filtro de jugador."},{"Deletes the entire fight, including all its players.","Borra el combate completo, incluyendo a todos sus jugadores."},
        {"One row per player. Scroll the table horizontally to see all statistics.","Una fila por jugador. Desliza la tabla horizontalmente para ver todas las estadísticas."},{"Player filter","Filtro de jugador"},
        {"Configuration","Configuración"},{"History","Registro"},{"Combat history","Registro de combates"},{"STORAGE","ALMACENAMIENTO"},
        {"Automatically delete old records","Borrar registros antiguos automáticamente"},{"Delete records older than","Borrar registros con más de"},{"Maximum records","Máximo de registros"},{"Hours","Horas"},{"Days","Días"},
        {"Export TXT","Exportar TXT"},{"Export combat record","Exportar registro de combate"},{"Delete record","Borrar registro"},{"Clear all history","Borrar todos los registros"},{"Saved fights","Combates guardados"},{"History storage error","Error al guardar el registro"},{"Record exported.","Registro exportado."},{"Export failed","Error de exportación"},
        {"Started","Inicio"},{"Ended","Fin"},{"Duration","Duración"},{"Players","Jugadores"},{"End reason","Motivo del cierre"},{"Map ID","ID del mapa"},{"Self ID","Tu ID"},{"Party IDs","IDs del grupo"},{"Actor ID","ID del jugador"},{"Role","Rol"},{"Party known","Grupo identificado"},{"Recovered party","Grupo recuperado"},{"Widget filter","Filtro del widget"},{"Tank","Tanque"},{"Healer / support","Sanador / apoyo"},{"Unknown","Desconocido"},
        {"Inactivity","Inactividad"},{"Manual reset","Reinicio manual"},{"Connection or area change","Cambio de conexión o zona"},{"Meter disabled","Medidor desactivado"},{"Helper closed","Helper cerrado"},
        {"Periodic damage","Daño periódico"},{"Impacts","Impactos"},{"Critical damage impacts","Impactos críticos de daño"},{"Eligible damage impacts","Impactos de daño evaluados"},{"Primary damage","Daño principal"},{"Critical primary damage","Daño principal crítico"},
        {"Damage crit. %","Daño crít. %"},{"Damage crit. rate","Frec. crít. de daño"},{"Healing crit. %","Curación crít. %"},{"Healing crit. rate","Frec. crít. de curación"},{"Received crit. %","Recibida crít. %"},{"Received crit. rate","Frec. crít. recibida"},{"Received HPS","HPS recibida"},
        {"Eligible healing impacts","Curas evaluadas"},{"Critical healing impacts","Curas críticas"},{"Primary healing","Curación principal"},{"Critical primary healing","Curación principal crítica"},{"Eligible received impacts","Curas recibidas evaluadas"},{"Critical received impacts","Curas recibidas críticas"},{"Primary received healing","Curación recibida principal"},{"Critical primary received healing","Curación recibida principal crítica"},
        {"History includes all identified players received by your game, independently of the widget filter.","El registro incluye todos los jugadores identificados cuyos datos recibe tu juego, independientemente del filtro del widget."},
        {"Diagnostic counters are cumulative for the reader session.","Los contadores de diagnóstico son acumulados de la sesión del lector."},
        {"No saved fights yet. Fight while the DPS meter is enabled, then wait for its combat timeout.","Todavía no hay combates guardados. Combate con el medidor activado y espera el tiempo de inactividad configurado."},
        {"Partial metrics. Critical percentages use eligible primary direct impacts; healing is raw restoration. Unknown effects, effective healing, overhealing and unvalidated boss mechanics are not included.","Métricas parciales. Los porcentajes críticos usan impactos directos principales evaluados; la curación es la cantidad anunciada. No incluye efectos desconocidos, curación efectiva, exceso de curación ni mecánicas de jefe sin validar."},
        {"Each fight is saved after the configured combat inactivity timeout. Resets, area changes and closing the helper save the unfinished fight with its end reason. Records survive restarts and include all identified players, regardless of the widget filter. At the storage limit, the oldest record is removed. Optional age cleanup runs at startup and once a minute. Deleting records cannot be undone.","Cada combate se guarda al superar el tiempo de inactividad configurado. Los reinicios, cambios de zona y el cierre del helper guardan el combate pendiente indicando el motivo. Los registros se conservan al reiniciar e incluyen a todos los jugadores identificados, sin depender del filtro del widget. Al alcanzar el límite se elimina el más antiguo. El borrado opcional por antigüedad se comprueba al iniciar y cada minuto. Borrar registros no se puede deshacer."},
        {"Heals","Curas"},{"Received","Recibidas"},
        {"Crit. %","Crít. %"},{"Crit. rate","Frec. crít."},
        {"Partial damage · identified DoT included","Daño parcial · daño periódico identificado incluido"},{"Raw healing · supported skills only","Curación anunciada · habilidades compatibles"},
        {"Other players remain sorted by the selected combat view.","Los demás jugadores se ordenan según la vista de combate."},
        {"Crit. %: share of primary direct damage or healing from criticals. Crit. rate: observed critical frequency. Additional impacts and periodic ticks are excluded from both percentages.","Crít. %: proporción del daño o la curación directa principal que procede de críticos. Frec. crít.: frecuencia observada de críticos. Los impactos adicionales y los ticks periódicos se excluyen de ambos porcentajes."},
        {"Alpha: partial damage and raw healing from supported skills. Identified damage-over-time effects are included automatically. Other effects, effective healing and overhealing are still being validated. Nearby covers only combat data received by your game.","Alpha: daño parcial y curación anunciada de habilidades compatibles. El daño periódico identificado se incluye automáticamente. Otros efectos, la curación efectiva y el exceso de curación siguen en validación. Cercanos incluye solo los datos de combate que recibe tu juego."},
        {"Name","Nombre"},{"Damage","Daño"},{"You","Tú"},{"Your character name","Nombre de tu personaje"},{"Optional · leave empty for automatic detection.","Opcional · déjalo vacío para detectar automáticamente."},
        {"Show my name as You","Mostrar mi nombre como Tú"},{"Uses the language selected in General settings.","Usa el idioma seleccionado en los ajustes generales."},{"Keep my character first","Mostrar mi personaje siempre primero"},{"Other players remain sorted by damage.","Los demás jugadores siguen ordenados por daño."},
        {"End encounter after idle","Terminar combate tras inactividad"},{"Experimental Fire Wall ticks","Ticks experimentales de Barrera de fuego"},
        {"Exact periodic damage is still being validated. Changing this starts a new encounter.","El daño periódico exacto sigue en pruebas. Cambiar esto inicia un combate nuevo."},
        {"Alpha: partial damage. Other periodic effects and summons are not yet supported. Nearby covers only players whose combat data reaches your game.","Alpha: daño parcial. Otros efectos periódicos e invocaciones aún no son compatibles. Cercanos incluye a jugadores cuyos datos de combate llegan a tu juego."},
        {"Reset encounter","Reiniciar combate"},{"Bar color","Color de las barras"},{"Visible rows","Filas visibles"},{"DPS disabled","DPS desactivado"},
        {"Waiting for combat data","Esperando datos de combate"},{"Dash once to identify your character","Haz un dash para identificar tu personaje"},
        {"Party not detected · showing yourself","Grupo sin detectar · se muestra solo tu personaje"},{"Waiting for combat","Esperando combate"},{"In combat","En combate"},{"Last encounter","Último combate"},
        {"Includes experimental ticks","Incluye ticks experimentales"},{"Partial damage · DoT excluded","Daño parcial · daño periódico excluido"},
        {"Colors by class role","Colores según el rol de la clase"},{"Damage color","Color de DPS"},{"Healer / support color","Color de curación / apoyo"},{"Tank color","Color de tanque"},{"Fallback bar color","Color sin clase identificada"},
        {"Tank: Gladiator / Templar. Healer / support: Cleric / Chanter. Other classes: damage.","Tanque: Gladiador / Templario. Curación / apoyo: Clérigo / Cantor. Otras clases: DPS."},
        {"VISIBILITY & ANIMATION","VISIBILIDAD Y ANIMACIÓN"},{"Hide automatically out of combat","Ocultar automáticamente fuera de combate"},{"Hide after inactivity","Ocultar tras inactividad"},
        {"Uses the players selected by your filter. Unlocked widgets stay visible.","Se aplica a los jugadores de tu filtro. Al desbloquear, el widget permanece visible."},
        {"Smooth bar movement","Suavizar movimiento de las barras"},{"Bar smoothing time","Tiempo de suavizado de las barras"},
        {"◉   FPS Counter","◉   Contador de FPS"},
        {"FPS Counter","Contador de FPS"},
        {"Your game frame rate, at a glance","Los fotogramas del juego, de un vistazo"},
        {"Enable FPS counter","Activar contador de FPS"},
        {"Sample value · not a game reading","Valor de ejemplo · no es una lectura del juego"},
        {"APPEARANCE","APARIENCIA"},
        {"Text color","Color del texto"},
        {"FPS text","Texto de FPS"},
        {"Dark background","Fondo oscuro"},
        {"A soft black background with transparent edges.","Fondo negro difuminado con bordes transparentes."},
        {"Background opacity","Opacidad del fondo"},
        {"Background softness","Difuminado del fondo"},
        {"Scale","Escala"},
        {"Height","Altura"},
        {"Reset FPS appearance","Restablecer aspecto de FPS"},
        {"MEASUREMENT","MEDICIÓN"},
        {"Refresh interval","Intervalo de actualización"},
        {"Start measurement as administrator","Iniciar medición como administrador"},
        {"FPS disabled","FPS desactivados"},
        {"Waiting for Aion 2","Esperando a Aion 2"},
        {"PresentMon is missing","Falta PresentMon"},
        {"Starting FPS measurement","Iniciando medición de FPS"},
        {"Measuring game FPS","Midiendo los FPS del juego"},
        {"Waiting for game frames","Esperando fotogramas del juego"},
        {"FPS measurement needs permission","La medición de FPS necesita permisos"},
        {"FPS measurement unavailable","Medición de FPS no disponible"},
        {"FPS permission request cancelled","Solicitud de permisos de FPS cancelada"},
        {"Default: 250 ms. Lower intervals update the number more often.\nWindows may deliver new readings about once per second.","Predeterminado: 250 ms. Un intervalo menor actualiza el número más a menudo.\nWindows puede entregar nuevas lecturas aproximadamente una vez por segundo."},
        {"General","General"},{"APPLICATION","APLICACIÓN"},{"◉   General","◉   General"},{"◉   Energy Bar","◉   Barra de energía"},
        {"Energy Bar","Barra de energía"},{"App preferences","Preferencias de la aplicación"},{"Language","Idioma"},
        {"Choose the language used by Aion 2 Helper.","Elige el idioma de Aion 2 Helper."},
        {"Changes apply immediately and are saved automatically.","Los cambios se aplican al instante y se guardan automáticamente."},
        {"Aion 2 Helper · Settings","Aion 2 Helper · Ajustes"},{"Aion 2 Helper · Menu","Aion 2 Helper · Menú"},{"Aion 2 Helper · Color","Aion 2 Helper · Color"},
        {"Settings","Ajustes"},{"SETTINGS","AJUSTES"},{"GENERAL STATUS","ESTADO GENERAL"},
        {"✓  Changes saved automatically","✓  Cambios guardados automáticamente"},{"Move and resize widgets directly on screen.","Mueve y ajusta los widgets directamente en pantalla."},
        {"Widgets locked","Widgets bloqueados"},{"Widgets unlocked","Widgets desbloqueados"},{"Lock","Bloquear"},{"Unlock","Desbloquear"},{"Quit Aion 2 Helper","Cerrar Aion 2 Helper"},
        {"Energy bar for dash and sprint","Barra de energía para dash y sprint"},{"Enable Energy Bar","Activar barra de energía"},{"Enabled","Activada"},{"Disabled","Desactivada"},
        {"PREVIEW","VISTA PREVIA"},{"Preview energy","Energía de la vista previa"},{"Behavior","Comportamiento"},{"Appearance","Apariencia"},{"Position & calibration","Posición y calibración"},
        {"↶  Undo","↶  Deshacer"},{"↷  Redo","↷  Rehacer"},{"Undo the last change · Ctrl+Z","Deshacer el último cambio · Ctrl+Z"},{"Redo the undone change · Ctrl+Y","Rehacer el cambio · Ctrl+Y"},{"Done","Listo"},
        {"Minimize Settings","Minimizar ajustes"},{"Close Settings","Cerrar ajustes"},{"VISIBILITY","VISIBILIDAD"},{"Auto-hide","Ocultar automáticamente"},
        {"Waits, then hides when energy is full.","Espera y se oculta al llegar al máximo de energía."},{"Delay at 100%","Espera al 100%"},
        {"ANIMATION","ANIMACIÓN"},{"Fade in and out","Aparición y desaparición gradual"},{"Smooth transitions when appearing and hiding.","Transiciones suaves al aparecer y ocultarse."},{"Fade duration","Duración de la transición"},
        {"Smooth energy changes","Suavizar cambios de energía"},{"Smooths energy use and recovery.","Suaviza el consumo y la recuperación de energía."},{"Smoothing time","Tiempo de suavizado"},
        {"PALETTE","PALETA"},{"Color by energy level","Color según la energía"},{"Gradual transitions between your three colors.","Transiciones graduales entre los tres colores."},
        {"High energy","Energía alta"},{"Medium energy","Energía media"},{"Low energy","Energía baja"},{"From 75%","Desde el 75%"},{"Up to 25%","Hasta el 25%"},{"35–65% · also used as the fixed color","35–65% · también se usa como color fijo"},
        {"LIGHT & CONTRAST","BRILLO Y CONTRASTE"},{"Emissive glow","Brillo emisivo"},{"A bright filament with a soft halo.","Un filamento luminoso con un halo suave."},{"Glow intensity","Intensidad del brillo"},{"Dark track opacity","Opacidad de la línea oscura"},{"Opacity of the background line.","Opacidad de la línea de fondo."},{"Reset effects and colors","Restablecer efectos y colores"},
        {"GEOMETRY","TAMAÑO Y POSICIÓN"},{"Width","Longitud"},{"Thickness","Grosor"},{"Horizontal position","Posición horizontal"},{"Vertical position","Posición vertical"},{"Reset size · 320 × 4","Restablecer tamaño · 320 × 4"},
        {"ENERGY CALIBRATION","CALIBRACIÓN DE ENERGÍA"},{"Character maximum","Máximo del personaje"},{"Reference value for 100% energy.","Valor de referencia para el 100% de energía."},{"Use current reading as maximum","Usar lectura actual como máximo"},
        {"No game reading. Enter the game, dash once, and wait for full energy before calibrating.","Sin lectura del juego. Entra con tu personaje, haz un dash y espera a tener la energía llena antes de calibrar."},
        {"Enter the game with your character and dash once to start receiving readings. Wait until your energy is completely full, then click the button to save that value as your maximum.\n\nThis uses your current energy; it does not automatically detect your maximum. Using it before energy is full will give an incorrect percentage. Only recalibrate if you change characters or the bar no longer matches the game.","Entra con tu personaje y haz un dash para empezar a recibir lecturas. Espera a tener la energía completamente llena y pulsa el botón para guardar ese valor como máximo.\n\nSe usa tu energía actual; el máximo no se detecta automáticamente. Si lo haces antes de llenar la barra, el porcentaje será incorrecto. Recalibra solo si cambias de personaje o la barra deja de coincidir con el juego."},
        {"Choose color","Elegir color"},{"Cancel color selection","Cancelar selección de color"},{"Previous color","Color anterior"},{"Cancel","Cancelar"},{"Apply color","Aplicar color"},{"HEX color","Color HEX"},{"Red","Rojo"},{"Green","Verde"},{"Blue","Azul"},
        {"Enter a valid HEX color, such as #B85416.","Introduce un color HEX válido, como #B85416."},{"RGB values must be between 0 and 255.","Los valores RGB deben estar entre 0 y 255."},
        {"Energy Bar disabled","Barra de energía desactivada"},{"No data · dash once to start","Sin datos · haz un dash para iniciar"},
        {"Enter the game with your character first: no Aion connections detected.","Entra con tu personaje antes de iniciar: no se detectan conexiones de Aion."},
        {"Npcap returned no adapters.","Npcap no ha encontrado adaptadores de red."},
        {"ENERGY BAR REQUIREMENT","REQUISITO DE LA BARRA DE ENERGÍA"},
        {"GAME DATA REQUIREMENT","REQUISITO PARA LOS DATOS DEL JUEGO"},
        {"Npcap is required for Energy Bar.","Npcap es necesario para la barra de energía."},
        {"Npcap is required for Energy Bar and DPS Meter.","Npcap es necesario para la barra de energía y el medidor de DPS."},
        {"Npcap could not be loaded. Reinstall Npcap for Energy Bar and DPS Meter.","No se ha podido cargar Npcap. Reinstálalo para la barra de energía y el medidor de DPS."},
        {"Install Npcap for Energy Bar and DPS Meter, then restart Aion 2 Helper. FPS Counter works without it.","Instala Npcap para la barra de energía y el medidor de DPS y reinicia Aion 2 Helper. El contador de FPS funciona sin él."},
        {"Npcap could not be loaded. Reinstall Npcap for Energy Bar.","No se ha podido cargar Npcap. Reinstálalo para la barra de energía."},
        {"Install Npcap, then restart Aion 2 Helper. FPS Counter works without it.","Instala Npcap y reinicia Aion 2 Helper. El contador de FPS funciona sin él."},
        {"Download Npcap","Descargar Npcap"},
        {"Could not open the browser. Download Npcap at https://npcap.com/#download","No se ha podido abrir el navegador. Descarga Npcap en https://npcap.com/#download"}
    };
    static readonly Dictionary<string,string> English=Reverse();
    static readonly string[] Prefixes={"Choose color: ","Decrease ","Increase "};
    static Dictionary<string,string> Reverse(){var map=new Dictionary<string,string>();foreach(var pair in Spanish)map[pair.Value]=pair.Key;return map;}
    internal static string Text(string text,string language){
        if(text==null)return null;string translated;if(English.TryGetValue(text,out translated))text=translated;
        if(Normalize(language)!="es")return text;if(Spanish.TryGetValue(text,out translated))return translated;
        foreach(string prefix in Prefixes)if(text.StartsWith(prefix,StringComparison.Ordinal))return (prefix=="Choose color: "?"Elegir color: ":prefix=="Decrease "?"Reducir ":"Aumentar ")+Text(text.Substring(prefix.Length),language);
        return text.Replace(" · recalibrate the maximum"," · recalibra el máximo");
    }
    sealed class Label {internal string Original,Rendered;}
    // Attached data lives only as long as its UI element, avoiding global window references.
    static readonly DependencyProperty LabelsProperty=DependencyProperty.RegisterAttached("Labels",typeof(Dictionary<DependencyProperty,Label>),typeof(UiLanguage));
    static void Translate(DependencyObject element,DependencyProperty property,string language){
        var current=element.GetValue(property) as string;if(current==null)return;
        var labels=(Dictionary<DependencyProperty,Label>)element.GetValue(LabelsProperty);if(labels==null){labels=new Dictionary<DependencyProperty,Label>();element.SetValue(LabelsProperty,labels);}
        Label label;if(!labels.TryGetValue(property,out label)){label=new Label{Original=Text(current,"en")};labels[property]=label;}else if(current!=label.Rendered)label.Original=Text(current,"en");
        label.Rendered=Text(label.Original,language);if(current!=label.Rendered)element.SetValue(property,label.Rendered);
    }
    internal static void Apply(DependencyObject window,string language){Walk(window,language,new HashSet<DependencyObject>());}
    static void Walk(DependencyObject element,string language,HashSet<DependencyObject> seen){
        if(!seen.Add(element))return;
        if(element is TextBlock)Translate(element,TextBlock.TextProperty,language);
        if(element is ContentControl)Translate(element,ContentControl.ContentProperty,language);
        if(element is FrameworkElement){Translate(element,FrameworkElement.ToolTipProperty,language);Translate(element,AutomationProperties.NameProperty,language);}
        if(element is Window)Translate(element,Window.TitleProperty,language);
        foreach(object child in LogicalTreeHelper.GetChildren(element)){var d=child as DependencyObject;if(d!=null)Walk(d,language,seen);}
    }
}
