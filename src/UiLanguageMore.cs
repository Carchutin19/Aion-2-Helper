using System;
using System.Collections.Generic;

// Reviewed interface vocabulary. Unknown/technical strings retain their English
// source rather than being blank or being translated on a frame/render thread.
internal static class UiLanguageMore {
 internal static readonly string[] Codes={"en","es","de","fr","ar"};
 internal static readonly string[] Names={"English","Español","Deutsch","Français","العربية"};
 static readonly Dictionary<string,Dictionary<string,string>> maps=new Dictionary<string,Dictionary<string,string>>();
 static readonly Dictionary<string,string> reverse=new Dictionary<string,string>();
 static UiLanguageMore(){for(int i=2;i<Codes.Length;i++)maps[Codes[i]]=new Dictionary<string,string>();foreach(string line in Data.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries)){var fields=line.Split('|');if(fields.Length!=4)throw new InvalidOperationException("Invalid interface translation row: "+fields[0]);for(int i=0;i<3;i++){maps[Codes[i+2]][fields[0]]=fields[i+1];if(!reverse.ContainsKey(fields[i+1]))reverse[fields[i+1]]=fields[0];}}}
 internal static bool TryText(string text,string language,out string result){Dictionary<string,string> map;result=null;return maps.TryGetValue(language,out map)&&map.TryGetValue(text,out result);}
 internal static bool TryEnglish(string text,out string result){return reverse.TryGetValue(text,out result);}
 internal static int Count(string language){Dictionary<string,string> map;return maps.TryGetValue(language,out map)?map.Count:0;}
 const string Data=@"Finishing diagnostic report|Diagnosebericht wird abgeschlossen|Finalisation du rapport de diagnostic|جارٍ إنهاء تقرير التشخيص
Starting diagnostics|Diagnose wird gestartet|Démarrage du diagnostic|جارٍ بدء التشخيص
Could not write the diagnostic report. Check that the Helper folder is writable.|Der Diagnosebericht konnte nicht gespeichert werden. Prüfe die Schreibrechte im Helper-Ordner.|Impossible d’enregistrer le rapport. Vérifiez les droits d’écriture du dossier du Helper.|تعذّر حفظ تقرير التشخيص. تأكد من إمكانية الكتابة في مجلد Helper.
Floating Text|Kampftext|Texte flottant|نصوص القتال
Decrease|Verringern|Diminuer|تقليل
Increase|Erhöhen|Augmenter|زيادة
General|Allgemein|Général|عام
APPLICATION|ANWENDUNG|APPLICATION|التطبيق
App preferences|App-Einstellungen|Préférences de l’application|تفضيلات التطبيق
Language|Sprache|Langue|اللغة
Choose the language used by Aion 2 Helper.|Wähle die Sprache von Aion 2 Helper.|Choisissez la langue d’Aion 2 Helper.|اختر لغة Aion 2 Helper.
Changes apply immediately and are saved automatically.|Änderungen werden sofort angewendet und automatisch gespeichert.|Les modifications sont appliquées immédiatement et enregistrées automatiquement.|تُطبّق التغييرات فورًا وتُحفظ تلقائيًا.
Some advanced help is still shown in English.|Einige erweiterte Hilfetexte sind noch auf Englisch.|Certaines aides avancées sont encore affichées en anglais.|بعض نصوص المساعدة المتقدمة ما زالت بالإنجليزية.
Settings|Einstellungen|Paramètres|الإعدادات
SETTINGS|EINSTELLUNGEN|PARAMÈTRES|الإعدادات
GENERAL STATUS|ALLGEMEINER STATUS|ÉTAT GÉNÉRAL|الحالة العامة
✓  Changes saved automatically|✓  Änderungen automatisch gespeichert|✓  Modifications enregistrées automatiquement|✓  تم حفظ التغييرات تلقائيًا
Lock widgets|Widgets sperren|Verrouiller les widgets|قفل عناصر الواجهة
Turn off to move and resize widgets on screen.|Deaktivieren, um Widgets auf dem Bildschirm zu verschieben und zu skalieren.|Désactivez pour déplacer et redimensionner les widgets à l’écran.|عطّله لتحريك عناصر الواجهة وتغيير حجمها على الشاشة.
Widgets locked|Widgets gesperrt|Widgets verrouillés|عناصر الواجهة مقفلة
Widgets unlocked|Widgets entsperrt|Widgets déverrouillés|عناصر الواجهة غير مقفلة
Lock|Sperren|Verrouiller|قفل
Unlock|Entsperren|Déverrouiller|فتح القفل
Done|Fertig|Terminé|تم
Cancel|Abbrechen|Annuler|إلغاء
Close|Schließen|Fermer|إغلاق
Settings saved|Einstellungen gespeichert|Paramètres enregistrés|تم حفظ الإعدادات
Minimize Settings|Einstellungen minimieren|Réduire les paramètres|تصغير الإعدادات
Close Settings|Einstellungen schließen|Fermer les paramètres|إغلاق الإعدادات
Aion 2 Helper · Settings|Aion 2 Helper · Einstellungen|Aion 2 Helper · Paramètres|Aion 2 Helper · الإعدادات
Aion 2 Helper · Menu|Aion 2 Helper · Menü|Aion 2 Helper · Menu|Aion 2 Helper · القائمة
Aion 2 Helper · Color|Aion 2 Helper · Farbe|Aion 2 Helper · Couleur|Aion 2 Helper · اللون
Quit Aion 2 Helper|Aion 2 Helper beenden|Quitter Aion 2 Helper|إغلاق Aion 2 Helper
↶  Undo|↶  Rückgängig|↶  Annuler|↶  تراجع
↷  Redo|↷  Wiederholen|↷  Rétablir|↷  إعادة
Undo the last change · Ctrl+Z|Letzte Änderung rückgängig · Strg+Z|Annuler la dernière modification · Ctrl+Z|التراجع عن آخر تغيير · Ctrl+Z
Redo the undone change · Ctrl+Y|Änderung wiederherstellen · Strg+Y|Rétablir la modification · Ctrl+Y|إعادة التغيير · Ctrl+Y
Play Aion 2|Aion 2 spielen|Jouer à Aion 2|تشغيل Aion 2
GAME LAUNCH|SPIELSTART|LANCEMENT DU JEU|تشغيل اللعبة
Detect game|Spiel erkennen|Détecter le jeu|اكتشاف اللعبة
Select game folder|Spielordner wählen|Choisir le dossier du jeu|اختيار مجلد اللعبة
Choose launcher|Launcher wählen|Choisir le lanceur|اختيار مشغّل اللعبة
Automatic launcher|Automatischer Launcher|Lanceur automatique|مشغّل تلقائي
Game not detected|Spiel nicht erkannt|Jeu non détecté|لم يتم اكتشاف اللعبة
Waiting for Aion 2|Warten auf Aion 2|En attente d’Aion 2|بانتظار Aion 2
GAME DATA REQUIREMENT|VORAUSSETZUNG FÜR SPIELDATEN|PRÉREQUIS DES DONNÉES DU JEU|متطلبات بيانات اللعبة
Download Npcap|Npcap herunterladen|Télécharger Npcap|تنزيل Npcap
Enabled|Aktiviert|Activé|مفعّل
Disabled|Deaktiviert|Désactivé|معطّل
Enable|Aktivieren|Activer|تفعيل
Disable|Deaktivieren|Désactiver|تعطيل
PREVIEW|VORSCHAU|APERÇU|معاينة
BEHAVIOR|VERHALTEN|COMPORTEMENT|السلوك
Behavior|Verhalten|Comportement|السلوك
APPEARANCE|DARSTELLUNG|APPARENCE|المظهر
Appearance|Darstellung|Apparence|المظهر
POSITION|POSITION|POSITION|الموضع
Position & size|Position und Größe|Position et taille|الموضع والحجم
VISIBILITY|SICHTBARKEIT|VISIBILITÉ|الظهور
GEOMETRY|GRÖSSE UND POSITION|TAILLE ET POSITION|الحجم والموضع
ANIMATION|ANIMATION|ANIMATION|الحركة
PALETTE|FARBEN|PALETTE|الألوان
Width|Breite|Largeur|العرض
Height|Höhe|Hauteur|الارتفاع
Thickness|Dicke|Épaisseur|السماكة
Horizontal position|Horizontale Position|Position horizontale|الموضع الأفقي
Vertical position|Vertikale Position|Position verticale|الموضع العمودي
Text color|Textfarbe|Couleur du texte|لون النص
Text size|Textgröße|Taille du texte|حجم النص
Text scale|Textskalierung|Échelle du texte|مقياس النص
Text opacity|Textdeckkraft|Opacité du texte|عتامة النص
Outline color|Konturfarbe|Couleur du contour|لون الإطار
Outline thickness|Konturstärke|Épaisseur du contour|سماكة الإطار
Show outline|Kontur anzeigen|Afficher le contour|إظهار الإطار
Opacity|Deckkraft|Opacité|العتامة
Background|Hintergrund|Arrière-plan|الخلفية
Dark background|Dunkler Hintergrund|Fond sombre|خلفية داكنة
Background color|Hintergrundfarbe|Couleur du fond|لون الخلفية
Backdrop blur|Hintergrundunschärfe|Flou d’arrière-plan|تمويه الخلفية
Glass tint|Glasfärbung|Teinte du verre|درجة لون الزجاج
Widget opacity|Widget-Deckkraft|Opacité du widget|عتامة العنصر
Whole widget scale|Gesamtskalierung des Widgets|Échelle complète du widget|مقياس العنصر بالكامل
Choose color|Farbe wählen|Choisir une couleur|اختيار اللون
Apply color|Farbe anwenden|Appliquer la couleur|تطبيق اللون
Previous color|Vorherige Farbe|Couleur précédente|اللون السابق
HEX color|HEX-Farbe|Couleur HEX|لون HEX
Red|Rot|Rouge|أحمر
Green|Grün|Vert|أخضر
Blue|Blau|Bleu|أزرق
Enter a valid HEX color, such as #B85416.|Gib eine gültige HEX-Farbe ein, z. B. #B85416.|Saisissez une couleur HEX valide, par exemple #B85416.|أدخل لون HEX صالحًا مثل #B85416.
RGB values must be between 0 and 255.|RGB-Werte müssen zwischen 0 und 255 liegen.|Les valeurs RVB doivent être comprises entre 0 et 255.|يجب أن تكون قيم RGB بين 0 و255.
Auto-hide|Automatisch ausblenden|Masquer automatiquement|إخفاء تلقائي
Fade in and out|Ein- und Ausblenden|Apparition et disparition progressives|ظهور واختفاء تدريجي
Fade duration|Überblenddauer|Durée du fondu|مدة التلاشي
Reset|Zurücksetzen|Réinitialiser|إعادة ضبط
Reset effects and colors|Effekte und Farben zurücksetzen|Réinitialiser les effets et couleurs|إعادة ضبط التأثيرات والألوان
Emissive glow|Leuchteffekt|Lueur émissive|التوهج
Glow intensity|Leuchtstärke|Intensité de la lueur|شدة التوهج
High energy|Hohe Energie|Énergie élevée|طاقة مرتفعة
Medium energy|Mittlere Energie|Énergie moyenne|طاقة متوسطة
Low energy|Niedrige Energie|Énergie faible|طاقة منخفضة
Energy Bar|Energieleiste|Barre d’énergie|شريط الطاقة
Enable Energy Bar|Energieleiste aktivieren|Activer la barre d’énergie|تفعيل شريط الطاقة
Energy bar for dash and sprint|Energieleiste für Ausweichen und Sprint|Barre d’énergie pour l’esquive et le sprint|شريط طاقة الاندفاع والركض
Energy Bar disabled|Energieleiste deaktiviert|Barre d’énergie désactivée|شريط الطاقة معطّل
No data · dash once to start|Keine Daten · einmal ausweichen|Aucune donnée · faites une esquive|لا توجد بيانات · اندفع مرة للبدء
Progress bar|Fortschrittsleisten|Barres de progression|أشرطة التقدم
Stagger Bar|Taumelleiste|Barre de déstabilisation|شريط الترنّح
Enable Stagger Bar|Taumelleiste aktivieren|Activer la barre de déstabilisation|تفعيل شريط الترنّح
Stagger|Taumeln|Déstabilisation|الترنّح
Test effect|Effekt testen|Tester l’effet|تجربة التأثير
STAGGERED EFFECT|TAUMELEFFEKT|EFFET DE DÉSTABILISATION|تأثير الترنّح
Text glow color|Text-Leuchtfarbe|Couleur de la lueur du texte|لون توهج النص
Effect color|Effektfarbe|Couleur de l’effet|لون التأثير
Our target|Unser Ziel|Notre cible|هدفنا
Target of target|Ziel des Ziels|Cible de la cible|هدف الهدف
Enable our target|Unser Ziel aktivieren|Activer notre cible|تفعيل هدفنا
Enable target of target|Ziel des Ziels aktivieren|Activer la cible de la cible|تفعيل هدف الهدف
Player health|Spieler-Lebenspunkte|Vie du joueur|صحة اللاعب
Player mana|Spieler-Mana|Mana du joueur|مانا اللاعب
Enemy NPC|Feindlicher NPC|PNJ ennemi|شخصية غير لاعبة معادية
Enemy player|Feindlicher Spieler|Joueur ennemi|لاعب عدو
Neutral player|Neutraler Spieler|Joueur neutre|لاعب محايد
Neutral NPC|Neutraler NPC|PNJ neutre|شخصية غير لاعبة محايدة
Name color|Namensfarbe|Couleur du nom|لون الاسم
Bar color|Leistenfarbe|Couleur de la barre|لون الشريط
Show name|Namen anzeigen|Afficher le nom|إظهار الاسم
Show distance|Entfernung anzeigen|Afficher la distance|إظهار المسافة
Show level|Stufe anzeigen|Afficher le niveau|إظهار المستوى
Distance|Entfernung|Distance|المسافة
Name|Name|Nom|الاسم
Level|Stufe|Niveau|المستوى
Unknown|Unbekannt|Inconnu|غير معروف
FPS/Ping|FPS/Ping|FPS/Ping|الإطارات وزمن الاستجابة
FPS Counter|FPS-Zähler|Compteur de FPS|عداد الإطارات
Enable FPS Counter|FPS-Zähler aktivieren|Activer le compteur de FPS|تفعيل عداد الإطارات
MEASUREMENT|MESSUNG|MESURE|القياس
Refresh interval|Aktualisierungsintervall|Intervalle d’actualisation|فاصل التحديث
Copy FPS diagnostics|FPS-Diagnose kopieren|Copier le diagnostic FPS|نسخ تشخيص الإطارات
DPS Meter|DPS-Messung|Compteur de DPS|مقياس الضرر في الثانية
Enable DPS Meter|DPS-Messung aktivieren|Activer le compteur de DPS|تفعيل مقياس الضرر
Damage|Schaden|Dégâts|الضرر
Healing|Heilung|Soins|العلاج
Received|Erhalten|Reçus|المستلم
Damage dealt|Verursachter Schaden|Dégâts infligés|الضرر المُلحَق
Damage taken|Erlittener Schaden|Dégâts subis|الضرر المستلَم
Healing received|Erhaltene Heilung|Soins reçus|العلاج المستلَم
History|Verlauf|Historique|السجل
Clear history|Verlauf löschen|Effacer l’historique|مسح السجل
Delete|Löschen|Supprimer|حذف
Total|Gesamt|Total|الإجمالي
Critical|Kritisch|Critique|حرِج
Critical damage|Kritischer Schaden|Dégâts critiques|الضرر الحرج
Floating Combat Text|Schwebender Kampftext|Texte de combat flottant|نصوص القتال العائمة
Damage dealt, damage taken and received healing|Verursachter und erlittener Schaden sowie erhaltene Heilung|Dégâts infligés, dégâts subis et soins reçus|الضرر المُلحَق والمستلَم والعلاج المستلَم
Enable floating combat text|Schwebenden Kampftext aktivieren|Activer le texte de combat flottant|تفعيل نصوص القتال العائمة
Simulated numbers · critical hits end in !!|Simulierte Zahlen · kritische Treffer enden mit !!|Nombres simulés · les coups critiques se terminent par !!|أرقام تجريبية · تنتهي الضربات الحرجة بعلامة !!
Test floating text|Schwebenden Text testen|Tester le texte flottant|تجربة النصوص العائمة
Show this category|Diese Kategorie anzeigen|Afficher cette catégorie|إظهار هذه الفئة
Movement direction|Bewegungsrichtung|Sens du déplacement|اتجاه الحركة
Up|Nach oben|Vers le haut|إلى الأعلى
Down|Nach unten|Vers le bas|إلى الأسفل
Time on screen|Anzeigedauer|Durée à l’écran|مدة العرض
Travel distance|Bewegungsstrecke|Distance parcourue|مسافة الحركة
Horizontal spread|Horizontale Streuung|Dispersion horizontale|التشتت الأفقي
Vertical spacing|Vertikaler Abstand|Espacement vertical|التباعد العمودي
Spawn position X|Startposition X|Position de départ X|موضع الظهور X
Spawn position Y|Startposition Y|Position de départ Y|موضع الظهور Y
CRITICAL HITS|KRITISCHE TREFFER|COUPS CRITIQUES|الضربات الحرجة
Separate critical damage|Kritischen Schaden getrennt gestalten|Personnaliser les dégâts critiques|تخصيص الضرر الحرج بشكل منفصل
Critical text size|Kritische Textgröße|Taille du texte critique|حجم نص الضربة الحرجة
Critical text color|Kritische Textfarbe|Couleur du texte critique|لون نص الضربة الحرجة
Use an independent critical widget|Eigenes Widget für kritische Treffer|Utiliser un widget critique indépendant|استخدام عنصر مستقل للضربات الحرجة
Test critical hits|Kritische Treffer testen|Tester les coups critiques|تجربة الضربات الحرجة
Stop test|Test beenden|Arrêter le test|إيقاف التجربة
Give confirmed outgoing critical hits their own appearance. Each hit is displayed once; !! still marks criticals.|Gestalte bestätigte eigene kritische Treffer separat. Jeder Treffer erscheint nur einmal; !! kennzeichnet weiterhin kritische Treffer.|Personnalisez les coups critiques infligés confirmés. Chaque coup n’apparaît qu’une fois ; !! indique toujours les critiques.|خصّص مظهر ضرباتك الحرجة المؤكدة. تظهر كل ضربة مرة واحدة فقط، وتظل !! علامة الضربة الحرجة.
Off: criticals share the damage-dealt anchor, movement and collision spacing, with their own appearance. On: move and scale the critical widget independently; criticals are removed from the normal damage widget.|Aus: Kritische Treffer teilen Position, Bewegung und Abstände mit normalem Schaden, behalten aber ihr Aussehen. Ein: Das kritische Widget lässt sich separat verschieben und skalieren; kritische Treffer erscheinen nicht mehr im normalen Widget.|Désactivé : les critiques partagent l’ancrage, le mouvement et l’espacement des dégâts normaux, avec leur propre apparence. Activé : déplacez et redimensionnez leur widget séparément ; les critiques quittent le widget normal.|معطّل: تشترك الضربات الحرجة مع الضرر العادي في الموضع والحركة والتباعد، مع مظهرها الخاص. مفعّل: حرّك عنصر الضربات الحرجة وغيّر حجمه بشكل مستقل؛ ولن تظهر في عنصر الضرر العادي.
Unlock widgets in General to move or resize the critical widget on screen.|Entsperre Widgets unter Allgemein, um das kritische Widget zu verschieben oder zu skalieren.|Déverrouillez les widgets dans Général pour déplacer ou redimensionner celui des critiques.|افتح قفل العناصر في عام لتحريك عنصر الضربات الحرجة أو تغيير حجمه على الشاشة.
Pets|Begleiter|Familiers|المرافقون
Enable pet tracking|Begleiterverfolgung aktivieren|Activer le suivi des familiers|تفعيل تتبع المرافقين
Widget|Widget|Widget|عنصر الواجهة
Collection|Sammlung|Collection|المجموعة
Registry|Verzeichnis|Registre|السجل
Atlas|Atlas|Atlas|الأطلس
Search|Suchen|Rechercher|بحث
Completed|Abgeschlossen|Terminé|مكتمل
All|Alle|Tous|الكل
Pinned|Angeheftet|Épinglés|المثبتة
Pin|Anheften|Épingler|تثبيت
Unpin|Lösen|Désépingler|إلغاء التثبيت
Show soul widget|Seelen-Widget anzeigen|Afficher le widget des âmes|إظهار عنصر الأرواح
Notice duration|Anzeigedauer des Hinweises|Durée de l’alerte|مدة التنبيه
Visible cards|Sichtbare Karten|Cartes visibles|البطاقات الظاهرة
Notifications|Benachrichtigungen|Notifications|الإشعارات
Enable notifications|Benachrichtigungen aktivieren|Activer les notifications|تفعيل الإشعارات
Sound|Ton|Son|الصوت
Volume|Lautstärke|Volume|مستوى الصوت
Game Tweaks|Spielanpassungen|Réglages du jeu|تعديلات اللعبة
Crafting|Sammeln|Récolte|الجمع
Gathering resources and remaining uses|Ressourcen und verbleibende Versuche|Ressources et utilisations restantes|الموارد ومحاولات الجمع المتبقية
Enable gathering widget|Sammel-Widget aktivieren|Activer le widget de récolte|تفعيل عنصر جمع الموارد
Preview remaining uses|Verbleibende Versuche in der Vorschau|Utilisations restantes dans l’aperçu|المحاولات المتبقية في المعاينة
Show resource name|Ressourcennamen anzeigen|Afficher le nom de la ressource|إظهار اسم المورد
Show resource type|Ressourcentyp anzeigen|Afficher le type de ressource|إظهار نوع المورد
Show remaining count|Restliche Versuche anzeigen|Afficher le nombre restant|إظهار العدد المتبقي
Bar height|Leistenhöhe|Hauteur de la barre|ارتفاع الشريط
Segment spacing|Segmentabstand|Espacement des segments|التباعد بين المقاطع
Filled segment color|Farbe gefüllter Segmente|Couleur des segments remplis|لون المقاطع الممتلئة
Empty segment color|Farbe leerer Segmente|Couleur des segments vides|لون المقاطع الفارغة
Unknown resource|Unbekannte Ressource|Ressource inconnue|مورد غير معروف
Mineral|Mineral|Minerai|معدن
Gemstone|Edelstein|Gemme|حجر كريم
Herb|Kraut|Herbe|عشبة
Fibre|Faser|Fibre|ألياف
Hotbar|Aktionsleiste|Barre de compétences|شريط المهارات
Show Helper hotbar|Helper-Aktionsleiste anzeigen|Afficher la barre du Helper|إظهار شريط مهارات Helper
HOTBAR · EXPERIMENTAL|AKTIONSLEISTE · EXPERIMENTELL|BARRE DE COMPÉTENCES · EXPÉRIMENTALE|شريط المهارات · تجريبي
CHARACTER PRESET|CHARAKTERPROFIL|PROFIL DU PERSONNAGE|إعداد الشخصية
Test hotbar|Aktionsleiste testen|Tester la barre de compétences|تجربة شريط المهارات
Show slot labels|Slot-Beschriftungen anzeigen|Afficher les étiquettes des emplacements|إظهار تسميات الخانات
Slot size|Slot-Größe|Taille des emplacements|حجم الخانة
MOVE & SNAP|BEWEGUNG UND EINRASTEN|DÉPLACEMENT ET ALIGNEMENT|التحريك والمحاذاة
All together|Alles zusammen|Tout ensemble|الكل معًا
Snap on all sides|An allen Seiten einrasten|Alignement sur tous les côtés|المحاذاة من كل الجهات
Slot spacing|Slot-Abstand|Espacement des emplacements|التباعد بين الخانات
Snap distance|Einrastabstand|Distance d’alignement|مسافة المحاذاة
Columns|Spalten|Colonnes|الأعمدة
Arrange assigned slots|Zugewiesene Slots anordnen|Organiser les emplacements assignés|ترتيب الخانات المعيّنة
INDIVIDUAL SLOT|EINZELNER SLOT|EMPLACEMENT INDIVIDUEL|خانة فردية
Selected slot|Ausgewählter Slot|Emplacement sélectionné|الخانة المحددة
Hide this slot|Diesen Slot ausblenden|Masquer cet emplacement|إخفاء هذه الخانة
Slot label|Slot-Beschriftung|Étiquette de l’emplacement|تسمية الخانة
Slot horizontal offset|Horizontaler Slot-Versatz|Décalage horizontal de l’emplacement|إزاحة الخانة الأفقية
Slot vertical offset|Vertikaler Slot-Versatz|Décalage vertical de l’emplacement|إزاحة الخانة العمودية
Selected slot scale|Skalierung des ausgewählten Slots|Échelle de l’emplacement sélectionné|مقياس الخانة المحددة
LIVE COOLDOWN|AKTIVE ABKLINGZEIT|RECHARGE EN DIRECT|وقت الاستعادة المباشر
Show cooldown number|Abklingzeit anzeigen|Afficher le compteur de recharge|إظهار عداد الاستعادة
AION 2 ORIGINAL HOTBAR|ORIGINALE AION-2-AKTIONSLEISTE|BARRE D’ORIGINE D’AION 2|شريط مهارات AION 2 الأصلي
Create HUD without hotbar|HUD ohne Aktionsleiste erstellen|Créer un HUD sans barre de compétences|إنشاء واجهة دون شريط المهارات
Show created file|Erstellte Datei anzeigen|Afficher le fichier créé|إظهار الملف المُنشأ
SUPPORT & DIAGNOSTICS|SUPPORT UND DIAGNOSE|ASSISTANCE ET DIAGNOSTIC|الدعم والتشخيص
Play with diagnostics|Mit Diagnoseprotokoll spielen|Jouer avec le diagnostic|اللعب مع تسجيل التشخيص
Show support report|Supportbericht anzeigen|Afficher le rapport d’assistance|إظهار تقرير الدعم
Recording diagnostics|Diagnose wird aufgezeichnet|Enregistrement du diagnostic|جارٍ تسجيل التشخيص
Diagnostic recording finished|Diagnoseaufzeichnung beendet|Enregistrement du diagnostic terminé|انتهى تسجيل التشخيص
Diagnostics off|Diagnose deaktiviert|Diagnostic désactivé|التشخيص معطّل
Could not open the report folder.|Berichtsordner konnte nicht geöffnet werden.|Impossible d’ouvrir le dossier du rapport.|تعذّر فتح مجلد التقرير.
Only enable this if something is not working. Reproduce the problem while it is on, then turn it off to finish the report. For a connection or login problem, enable it before fully restarting Aion.|Nur bei einem Problem aktivieren. Stelle das Problem nach und deaktiviere die Option anschließend, um den Bericht fertigzustellen. Bei Verbindungs- oder Anmeldeproblemen vor einem vollständigen Neustart von Aion aktivieren.|Activez uniquement en cas de problème. Reproduisez-le pendant l’enregistrement, puis désactivez l’option pour terminer le rapport. Pour un problème de connexion, activez-la avant de redémarrer complètement Aion.|فعّله فقط عند وجود مشكلة. أعد حدوث المشكلة أثناء التفعيل، ثم عطّله لإنهاء التقرير. لمشكلات الاتصال أو الدخول، فعّله قبل إعادة تشغيل Aion بالكامل.
Send support-report.zip from the Helper folder privately to support, together with what failed and the steps you followed. No upload is automatic. Your settings are preserved.|Sende support-report.zip aus dem Helper-Ordner privat an den Support, zusammen mit dem Problem und deinen Schritten. Es wird nichts automatisch hochgeladen. Deine Einstellungen bleiben erhalten.|Envoyez support-report.zip depuis le dossier du Helper en privé à l’assistance, en décrivant le problème et les étapes suivies. Aucun envoi automatique. Vos réglages sont conservés.|أرسل support-report.zip من مجلد Helper بشكل خاص إلى الدعم، مع وصف المشكلة والخطوات التي اتبعتها. لا يتم رفع أي شيء تلقائيًا وتبقى إعداداتك محفوظة.
The report contains version, driver, connection counters and overlay/detection state, which may include character/server identifiers. It does not record packet contents, chat or session keys. Recording stops after 15 minutes or 8 MiB. Turn it off and on to start a new report; one previous ZIP is kept.|Der Bericht enthält Version, Treiber, Verbindungszähler und Erkennungs-/Widget-Status, eventuell mit Charakter-/Serverkennungen. Keine Paketdaten, Chats oder Sitzungsschlüssel. Die Aufzeichnung endet nach 15 Minuten oder 8 MiB. Aus- und einschalten startet einen neuen Bericht; eine vorherige ZIP bleibt erhalten.|Le rapport contient version, pilotes, compteurs de connexion et état des widgets/de la détection, éventuellement des identifiants de personnage/serveur. Aucun contenu de paquet, discussion ou clé de session. Arrêt après 15 minutes ou 8 Mio. Désactivez puis réactivez pour recommencer ; un ZIP précédent est conservé.|يتضمن التقرير الإصدار وبرامج التشغيل وعدادات الاتصال وحالة الواجهة والتعرّف، وقد يتضمن معرّفات الشخصية والخادم. لا يسجل محتوى الحزم أو المحادثات أو مفاتيح الجلسة. يتوقف بعد 15 دقيقة أو 8 ميبيبايت. عطّله وأعد تفعيله لبدء تقرير جديد؛ تُحفظ نسخة ZIP سابقة واحدة.
Updates|Updates|Mises à jour|التحديثات
Check for updates|Nach Updates suchen|Rechercher des mises à jour|البحث عن تحديثات
Download update|Update herunterladen|Télécharger la mise à jour|تنزيل التحديث
Install update|Update installieren|Installer la mise à jour|تثبيت التحديث
Up to date|Aktuell|À jour|محدّث
No character detected. Enter the game with the Helper open.|Kein Charakter erkannt. Betritt das Spiel mit geöffnetem Helper.|Aucun personnage détecté. Entrez dans le jeu avec le Helper ouvert.|لم يتم التعرّف على شخصية. ادخل اللعبة مع تشغيل Helper.
Only supported received impacts are shown. Healing values may include overhealing. Unknown critical flags do not add !!.|Nur unterstützte empfangene Treffer werden angezeigt. Heilwerte können Überheilung enthalten. Unbekannte Krit-Markierungen erhalten kein !!.|Seuls les impacts reçus pris en charge sont affichés. Les soins peuvent inclure du sursoin. Les critiques non confirmés n’ajoutent pas !!.|تُعرض فقط أحداث القتال المدعومة والمستلمة. قد تشمل قيم العلاج علاجًا زائدًا. لا تُضاف !! إذا لم تتأكد الضربة الحرجة.
Unlock widgets in General to drag each category. Drag its lower-right corner to scale it. Numbers appear at the anchor and move in the chosen direction.|Entsperre Widgets unter Allgemein, um Kategorien zu verschieben. Ziehe die rechte untere Ecke zum Skalieren. Zahlen erscheinen am Anker und bewegen sich in die gewählte Richtung.|Déverrouillez les widgets dans Général pour déplacer chaque catégorie. Faites glisser le coin inférieur droit pour redimensionner. Les nombres apparaissent à l’ancrage et se déplacent dans le sens choisi.|افتح قفل العناصر في عام لسحب كل فئة. اسحب الزاوية السفلية اليمنى لتغيير الحجم. تظهر الأرقام عند نقطة الارتكاز وتتحرك في الاتجاه المحدد.";
}
