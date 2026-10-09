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
        {"Npcap returned no adapters.","Npcap no ha encontrado adaptadores de red."}
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
