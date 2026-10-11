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
        {"WHAT'S NEW · v1.7.0 Alpha","NOVEDADES · v1.7.0 Alpha"},
        {"Notifications now distinguish normal party invitations from dungeon-group invitations, with the sender name and independent switches. Both share sound, fades, appearance and position. Existing settings are preserved.","Las notificaciones ya distinguen las invitaciones normales de las de grupo para dungeon, con el nombre del jugador y opciones independientes. Comparten sonido, fades, aspecto y posición. Se conservan los ajustes existentes."},
        {"Known issue: party members' damage may stop updating during open-world bosses. This remains unresolved; DPS totals and rankings can be incomplete.","Fallo conocido: el daño de compañeros del grupo puede dejar de actualizarse en jefes de mundo abierto. Sigue pendiente de solución; los totales y la clasificación de DPS pueden estar incompletos."},
        {"Minimum fight duration","Duración mínima del combate"},
        {"Highest hit","Golpe más alto"},
        {"Tagged direct impacts","Impactos directos con etiquetas"},
        {"Back","Espalda"},
        {"Front","Frente"},
        {"Double","Doble"},
        {"Perfect","Perfecto"},
        {"Back Critical","Espalda crítico"},
        {"Front Critical","Frente crítico"},
        {"Back Double Critical","Espalda doble crítico"},
        {"Front Double Critical","Frente doble crítico"},
        {"Back Perfect","Espalda perfecto"},
        {"Front Perfect","Frente perfecto"},
        {"Highest hit uses the largest primary damage impact or individual periodic tick recorded for that player; added extra-impact amounts are excluded. Tags describe primary direct impacts, can overlap and are unavailable for older records or missing metadata.","El golpe más alto es el mayor impacto principal de daño o tick periódico registrado para ese jugador; no suma los impactos adicionales. Las etiquetas describen impactos directos principales, pueden coincidir y no están disponibles en registros antiguos ni si faltan esos datos."},
        {"Player {0}","Jugador {0}"},
        {"◉   Notifications","◉   Notificaciones"},{"Notifications","Notificaciones"},{"Useful alerts while you play","Avisos útiles mientras juegas"},{"Enable notifications","Activar notificaciones"},
        {"PARTY INVITATIONS","INVITACIONES DE GRUPO"},{"Show party invitations","Mostrar invitaciones de grupo"},{"Play notification sound","Reproducir sonido"},{"Sound volume","Volumen del sonido"},{"Text size","Tamaño del texto"},{"Notification duration","Duración del aviso"},{"Test notification","Probar notificación"},
        {"Enable all notifications","Activar todas las notificaciones"},{"NOTIFICATION TYPES","TIPOS DE NOTIFICACIÓN"},{"Party invitations","Invitaciones al grupo"},{"COMMON SETTINGS","AJUSTES COMUNES"},
        {"Choose which notifications you want to receive. Turning all notifications off keeps your individual choices.","Elige qué notificaciones quieres recibir. Al desactivarlas todas se conservan tus selecciones."},
        {"Sound, duration, appearance and position apply to all notification types.","El sonido, la duración, el aspecto y la posición se comparten entre todas las notificaciones."},
        {"All notifications are disabled.","Todas las notificaciones están desactivadas."},{"Party invitations are disabled.","Las invitaciones al grupo están desactivadas."},
        {"Dungeon party invitations","Invitaciones al grupo de dungeon"},{"Dungeon party invitation","Invitación al grupo de dungeon"},{"{0} is inviting you to a dungeon party.","{0} te está invitando a un grupo de dungeon."},
        {"Party invitation","Invitación al grupo"},{"{0} is inviting you to a party.","{0} te está invitando al grupo."},{"Sample player","Jugador de ejemplo"},{"Sample notification · not a game event","Notificación de ejemplo · no es un evento del juego"},
        {"Notification","Notificación"},{"This is a sample notification.","Esto es una notificación de ejemplo."},
        {"Shugo Festival","Festival de Shugo"},{"Shugo Festival starts in 5 minutes.","El festival de Shugo empieza en 5 minutos."},
        {"Shugo Festival · 5 minutes before","Festival de Shugo · 5 minutos antes"},{"Test Shugo reminder","Probar aviso de Shugo"},
        {"Shugo Festival · registration opens","Festival de Shugo · apertura de inscripción"},
        {"You can now sign up for Shugo Festival.","Ya puedes apuntarte al festival de Shugo."},
        {"Test 5-minute reminder","Probar aviso de 5 minutos"},{"Test opening reminder","Probar aviso de apertura"},
        {"Reminders at :55 and :00 while Aion 2 is open. Turn each one on or off independently.","Avisos a los :55 y a la hora en punto mientras Aion 2 está abierto. Puedes activar o desactivar cada uno por separado."},
        {"Hourly Shugo reminders enabled.","Avisos horarios de Shugo activados."},
        {"Hourly reminder at :55 while Aion 2 is open. Uses the hourly :00 schedule and your computer clock.","Aviso a los :55 mientras Aion 2 está abierto. Sigue el horario de cada hora en punto y el reloj del PC."},
        {"Party invitations and hourly Shugo reminders enabled.","Invitaciones al grupo y avisos horarios de Shugo activados."},
        {"Hourly Shugo reminder enabled (:55).","Aviso horario de Shugo activado (:55)."},{"No notification types enabled.","No hay tipos de notificación activados."},
        {"Invitation detection is awaiting a live test.","Detección de invitaciones pendiente de la prueba en el juego."},{"Listening for party invitations.","Esperando invitaciones de grupo."},{"Notification sound file is missing.","No se encuentra el archivo de sonido."},{"Notification sound could not be played.","No se ha podido reproducir el sonido."},
        {"POSITION & SIZE","POSICIÓN Y TAMAÑO"},{"Unlock widgets to move or resize the notification area. A sample stays visible while editing; it does not play a sound.","Desbloquea los widgets para mover o ajustar el área de avisos. Al editar se muestra un ejemplo sin reproducir sonido."},
        {"◉   DPS Meter","◉   Medidor de DPS"},{"DPS Meter","Medidor de DPS"},{"Combat damage · experimental alpha","Daño en combate · versión alpha experimental"},
        {"Enable DPS meter","Activar medidor de DPS"},{"Self","Solo tú"},{"Party","Grupo"},{"Nearby players","Jugadores cercanos"},{"Show players","Mostrar jugadores"},
        {"Combat view","Vista de combate"},{"Healing","Curación"},{"Healing done","Curación realizada"},{"Healing received","Curación recibida"},
        {"Yes","Sí"},{"No","No"},{"All players","Todos los jugadores"},{"Filter by player","Filtrar por jugador"},{"Exports the selected fight using the player filter.","Exporta el combate seleccionado aplicando el filtro de jugador."},{"Deletes the entire fight, including all its players.","Borra el combate completo, incluyendo a todos sus jugadores."},
        {"One row per player. Scroll the table horizontally to see all statistics.","Una fila por jugador. Desliza la tabla horizontalmente para ver todas las estadísticas."},{"Player filter","Filtro de jugador"},
        {"Configuration","Configuración"},{"History","Registro"},{"Combat history","Registro de combates"},{"STORAGE","ALMACENAMIENTO"},
        {"Automatically delete old records","Borrar registros antiguos automáticamente"},{"Delete records older than","Borrar registros con más de"},{"Maximum records","Máximo de registros"},{"Hours","Horas"},{"Days","Días"},
        {"Export TXT","Exportar TXT"},{"Export combat record","Exportar registro de combate"},{"Delete record","Borrar registro"},{"Delete fight","Borrar combate"},{"Delete player","Borrar jugador"},{"Actions","Acciones"},{"Remove this player from this fight","Borrar este jugador de este combate"},{"Delete fight removes that whole fight, even with a player filter. Delete player removes only that player from that fight; removing its last player deletes the empty fight.","Borrar combate elimina esa pelea completa, aunque haya un filtro de jugador. Borrar jugador elimina solo ese jugador de esa pelea; si era el último, también elimina el combate vacío."},{"Clear all history","Borrar todos los registros"},{"Saved fights","Combates guardados"},{"History storage error","Error al guardar el registro"},{"Record exported.","Registro exportado."},{"Export failed","Error de exportación"},
        {"Started","Inicio"},{"Ended","Fin"},{"Duration","Duración"},{"Players","Jugadores"},{"End reason","Motivo del cierre"},{"Map ID","ID del mapa"},{"Self ID","Tu ID"},{"Party IDs","IDs del grupo"},{"Actor ID","ID del jugador"},{"Role","Rol"},{"Party known","Grupo identificado"},{"Recovered party","Grupo recuperado"},{"Widget filter","Filtro del widget"},{"Tank","Tanque"},{"Healer / support","Sanador / apoyo"},{"Unknown","Desconocido"},
        {"Inactivity","Inactividad"},{"Manual reset","Reinicio manual"},{"Connection or area change","Cambio de conexión o zona"},{"Meter disabled","Medidor desactivado"},{"Helper closed","Helper cerrado"},
        {"Periodic damage","Daño periódico"},{"Impacts","Impactos"},{"Critical damage impacts","Impactos críticos de daño"},{"Eligible damage impacts","Impactos de daño evaluados"},{"Primary damage","Daño principal"},{"Critical primary damage","Daño principal crítico"},
        {"Damage crit. %","Daño crít. %"},{"Damage crit. rate","Frec. crít. de daño"},{"Healing crit. %","Curación crít. %"},{"Healing crit. rate","Frec. crít. de curación"},{"Received crit. %","Recibida crít. %"},{"Received crit. rate","Frec. crít. recibida"},{"Received HPS","HPS recibida"},
        {"Eligible healing impacts","Curas evaluadas"},{"Critical healing impacts","Curas críticas"},{"Primary healing","Curación principal"},{"Critical primary healing","Curación principal crítica"},{"Eligible received impacts","Curas recibidas evaluadas"},{"Critical received impacts","Curas recibidas críticas"},{"Primary received healing","Curación recibida principal"},{"Critical primary received healing","Curación recibida principal crítica"},
        {"History includes all identified players received by your game, independently of the widget filter.","El registro incluye todos los jugadores identificados cuyos datos recibe tu juego, independientemente del filtro del widget."},
        {"Diagnostic counters are cumulative for the reader session.","Los contadores de diagnóstico son acumulados de la sesión del lector."},
        {"No saved fights yet. Reach the minimum fight duration with the DPS meter enabled, then wait for its combat timeout.","Aún no hay combates guardados. Alcanza la duración mínima con el medidor de DPS activado y espera a que termine por inactividad."},
        {"Partial metrics. Critical percentages use eligible primary direct impacts; healing is raw restoration. Unknown effects, effective healing, overhealing and unvalidated boss mechanics are not included.","Métricas parciales. Los porcentajes críticos usan impactos directos principales evaluados; la curación es la cantidad anunciada. No incluye efectos desconocidos, curación efectiva, exceso de curación ni mecánicas de jefe sin validar."},
        {"Only fights reaching the minimum duration are saved, measured from the first to the last recorded impact; waiting for the inactivity timeout does not add combat time. Set 0 seconds to save every fight. The filter also applies on resets, area changes and closing the helper, and does not delete existing records. Records survive restarts and include all identified players, regardless of the widget filter. At the storage limit, the oldest record is removed. Optional age cleanup runs at startup and once a minute. Deleting records cannot be undone.","Solo se guardan los combates que alcanzan la duración mínima, desde el primer hasta el último impacto registrado; la espera de inactividad no suma tiempo de combate. Pon 0 segundos para guardar todos. El filtro también se aplica al reiniciar, cambiar de zona y cerrar el helper, y no borra registros existentes. Los registros se conservan al reiniciar e incluyen a todos los jugadores identificados, sin depender del filtro del widget. Al alcanzar el límite se elimina el más antiguo. El borrado opcional por antigüedad se comprueba al iniciar y cada minuto. Borrar registros no se puede deshacer."},
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
        {"PREVIEW","VISTA PREVIA"},{"Preview energy","Energía de la vista previa"},{"Behavior","Comportamiento"},{"Appearance","Apariencia"},{"Position & size","Posición y tamaño"},
        {"↶  Undo","↶  Deshacer"},{"↷  Redo","↷  Rehacer"},{"Undo the last change · Ctrl+Z","Deshacer el último cambio · Ctrl+Z"},{"Redo the undone change · Ctrl+Y","Rehacer el cambio · Ctrl+Y"},{"Done","Listo"},
        {"Minimize Settings","Minimizar ajustes"},{"Close Settings","Cerrar ajustes"},{"VISIBILITY","VISIBILIDAD"},{"Auto-hide","Ocultar automáticamente"},
        {"Waits, then hides when energy is full.","Espera y se oculta al llegar al máximo de energía."},{"Delay at 100%","Espera al 100%"},
        {"ANIMATION","ANIMACIÓN"},{"Fade in and out","Aparición y desaparición gradual"},{"Smooth transitions when appearing and hiding.","Transiciones suaves al aparecer y ocultarse."},{"Fade duration","Duración de la transición"},
        {"Smooth energy changes","Suavizar cambios de energía"},{"Smooths energy use and recovery.","Suaviza el consumo y la recuperación de energía."},{"Smoothing time","Tiempo de suavizado"},
        {"PALETTE","PALETA"},{"Color by energy level","Color según la energía"},{"Gradual transitions between your three colors.","Transiciones graduales entre los tres colores."},
        {"High energy","Energía alta"},{"Medium energy","Energía media"},{"Low energy","Energía baja"},{"From 75%","Desde el 75%"},{"Up to 25%","Hasta el 25%"},{"35–65% · also used as the fixed color","35–65% · también se usa como color fijo"},
        {"LIGHT & CONTRAST","BRILLO Y CONTRASTE"},{"Emissive glow","Brillo emisivo"},{"A bright filament with a soft halo.","Un filamento luminoso con un halo suave."},{"Glow intensity","Intensidad del brillo"},{"Dark track opacity","Opacidad de la línea oscura"},{"Opacity of the background line.","Opacidad de la línea de fondo."},{"Reset effects and colors","Restablecer efectos y colores"},
        {"GEOMETRY","TAMAÑO Y POSICIÓN"},{"Width","Longitud"},{"Thickness","Grosor"},{"Horizontal position","Posición horizontal"},{"Vertical position","Posición vertical"},{"Reset size · 320 × 4","Restablecer tamaño · 320 × 4"},
        {"Choose color","Elegir color"},{"Cancel color selection","Cancelar selección de color"},{"Previous color","Color anterior"},{"Cancel","Cancelar"},{"Apply color","Aplicar color"},{"HEX color","Color HEX"},{"Red","Rojo"},{"Green","Verde"},{"Blue","Azul"},
        {"Enter a valid HEX color, such as #B85416.","Introduce un color HEX válido, como #B85416."},{"RGB values must be between 0 and 255.","Los valores RGB deben estar entre 0 y 255."},
        {"Energy Bar disabled","Barra de energía desactivada"},{"No data · dash once to start","Sin datos · haz un dash para iniciar"},
        {"Enter the game with your character first: no Aion connections detected.","Entra con tu personaje antes de iniciar: no se detectan conexiones de Aion."},
        {"Npcap returned no adapters.","Npcap no ha encontrado adaptadores de red."},
        {"ENERGY BAR REQUIREMENT","REQUISITO DE LA BARRA DE ENERGÍA"},
        {"GAME DATA REQUIREMENT","REQUISITO PARA LOS DATOS DEL JUEGO"},
        {"Npcap is required for Energy Bar.","Npcap es necesario para la barra de energía."},
        {"Npcap is required for Energy Bar, DPS Meter and Notifications.","Npcap es necesario para la barra de energía, el medidor de DPS y las notificaciones."},
        {"Npcap could not be loaded. Reinstall Npcap for Energy Bar, DPS Meter and Notifications.","No se ha podido cargar Npcap. Reinstálalo para la barra de energía, el medidor de DPS y las notificaciones."},
        {"Install Npcap for Energy Bar, DPS Meter and Notifications, then restart Aion 2 Helper. FPS Counter works without it.","Instala Npcap para la barra de energía, el medidor de DPS y las notificaciones y reinicia Aion 2 Helper. El contador de FPS funciona sin él."},
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
        return text.Replace(" · waiting for maximum energy stats"," · esperando el máximo del juego");
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
