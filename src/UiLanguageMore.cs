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
 const string Data=@"Snap widgets together|Widgets aneinander ausrichten|Aligner les widgets entre eux|محاذاة الأدوات مع بعضها
Align the edges and centers of nearby widgets while dragging. Hotbar slot snap is configured separately.|Beim Ziehen Kanten und Mittelpunkte benachbarter Widgets ausrichten. Das Einrasten der Hotbar-Plätze wird separat eingestellt.|Aligne les bords et les centres des widgets proches pendant le déplacement. L’alignement des emplacements de la barre de raccourcis se règle séparément.|حاذِ حواف الأدوات القريبة ومراكزها أثناء السحب. يُضبط التقاط خانات شريط المهارات بشكل مستقل.
Show alignment grid|Ausrichtungsraster anzeigen|Afficher la grille d’alignement|إظهار شبكة المحاذاة
Grid opacity|Rasterdeckkraft|Opacité de la grille|عتامة الشبكة
Show a grid with highlighted center lines on each screen while widgets are unlocked.|Bei entsperrten Widgets ein Raster mit hervorgehobenen Mittellinien auf jedem Bildschirm anzeigen.|Affiche une grille avec des axes centraux renforcés sur chaque écran lorsque les widgets sont déverrouillés.|اعرض شبكة بخطوط مركزية بارزة على كل شاشة عند إلغاء قفل الأدوات.
Show widgets only when Aion is in the foreground|Widgets nur anzeigen, wenn Aion im Vordergrund ist|Afficher les widgets uniquement quand Aion est au premier plan|عرض الأدوات فقط عندما يكون Aion في المقدمة
Unlocked widgets stay visible so you can move and resize them.|Entsperrte Widgets bleiben sichtbar, damit du sie verschieben und skalieren kannst.|Les widgets déverrouillés restent visibles pour que tu puisses les déplacer et les redimensionner.|تظل الأدوات غير المقفلة مرئية لتتمكن من نقلها وتغيير حجمها.
Flight Energy|Flugenergie|Énergie de vol|طاقة الطيران
Enable Flight Energy|Flugenergieleiste aktivieren|Activer la barre d’énergie de vol|تفعيل شريط طاقة الطيران
Flight energy and recovery|Flugenergie und Regeneration|Énergie de vol et récupération|طاقة الطيران واستعادتها
Show only while recharging|Nur beim Aufladen anzeigen|Afficher uniquement pendant la recharge|العرض أثناء الشحن فقط
Shows when flight energy starts recovering. Unlocked widgets stay visible.|Wird angezeigt, sobald sich die Flugenergie regeneriert. Entsperrte Widgets bleiben sichtbar.|S’affiche quand l’énergie de vol commence à se régénérer. Les widgets déverrouillés restent visibles.|يظهر عند بدء استعادة طاقة الطيران. تظل الأدوات غير المقفلة مرئية.
If flight energy is missing, open the Helper before entering your character so it can receive the maximum.|Falls die Flugenergie fehlt, öffne den Helper vor dem Einloggen mit deinem Charakter, damit er den Maximalwert empfängt.|Si l’énergie de vol manque, ouvre le Helper avant de connecter ton personnage pour recevoir la valeur maximale.|إذا كانت بيانات طاقة الطيران مفقودة، افتح Helper قبل الدخول بشخصيتك لتلقي القيمة القصوى.
Flight Energy disabled|Flugenergieleiste deaktiviert|Barre d’énergie de vol désactivée|شريط طاقة الطيران معطّل
Waiting for flight energy data|Warte auf Flugenergiedaten|En attente des données d’énergie de vol|بانتظار بيانات طاقة الطيران
Finishing diagnostic report|Diagnosebericht wird abgeschlossen|Finalisation du rapport de diagnostic|جارٍ إنهاء تقرير التشخيص
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
Waiting for maximum energy stats|Warte auf die maximale Energie des Charakters|En attente de l’énergie maximale du personnage|في انتظار الحد الأقصى لطاقة الشخصية
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
Time left|Restzeit|Restant|الوقت المتبقي
Boss|Boss|Boss|الزعيم
Target|Ziel|Cible|الهدف
Show estimated time remaining|Geschätzte Restzeit anzeigen|Afficher le temps restant estimé|إظهار الوقت المتبقي المقدّر
Uses recent health changes for the boss or selected enemy.|Basiert auf dem jüngsten Lebensverlust des Bosses oder des gewählten Gegners.|Se base sur la perte récente de vie du boss ou de l’ennemi choisi.|يُحسب من انخفاض نقاط الصحة مؤخرًا للزعيم أو العدو المحدد.
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
Back|Rücken|Dos|من الخلف
Front|Frontal|Frontal|من الأمام
Double|Doppelt|Double|مزدوج
Perfect|Perfekt|Parfait|مثالي
HIT LABEL|TREFFERBEZEICHNUNG|TYPE DE COUP|نوع الضربة
◉   Combat Log|◉   Kampflog|◉   Journal de combat|◉   سجل القتال
◉   Hidden Cube Radar|◉   Radar für versteckte Würfel|◉   Radar de cubes cachés|◉   رادار المكعبات المخفية
Hidden Cube Radar|Radar für versteckte Würfel|Radar de cubes cachés|رادار المكعبات المخفية
Nearby hidden cubes|Versteckte Würfel in der Nähe|Cubes cachés à proximité|المكعبات المخفية القريبة
Enable Hidden Cube Radar|Würfelradar aktivieren|Activer le radar de cubes cachés|تفعيل رادار المكعبات المخفية
Shows an alert when the game reports a supported nearby cube.|Meldet vom Spiel erkannte Würfel in der Nähe.|Signale les cubes proches pris en charge et détectés par le jeu.|ينبه عند اكتشاف اللعبة مكعبًا قريبًا مدعومًا.
Sample hidden cube|Beispielwürfel|Exemple de cube caché|مثال لمكعب مخفي
Hidden cube|Versteckter Würfel|Cube caché|مكعب مخفي
Detected|Erkannt|Détecté|تم الاكتشاف
Move to refresh your position|Bewege dich, um deine Position zu aktualisieren|Déplacez-vous pour actualiser votre position|تحرك لتحديث موقعك
Move to locate cube|Bewege dich, um den Würfel zu orten|Déplacez-vous pour localiser le cube|تحرك لتحديد موقع المكعب
Performance|Leistung|Performances|الأداء
Frame pacing test|Test der Bildrate|Test de fluidité|اختبار سلاسة الإطارات
FPS limit|FPS-Limit|Limite d’IPS|حد الإطارات في الثانية
Custom FPS limit|Eigenes FPS-Limit|Limite d’IPS personnalisée|حد مخصص للإطارات في الثانية
Use game settings|Spieleinstellungen|Paramètres du jeu|إعدادات اللعبة
Run the same route twice to compare FPS and slow frames. Return to Aion 2 after starting the test and keep it in focus.|Laufe dieselbe Strecke zweimal, um FPS und langsame Bilder zu vergleichen. Kehre nach dem Start zu Aion 2 zurück und lasse das Spiel im Vordergrund.|Parcourez le même trajet deux fois pour comparer les IPS et les images lentes. Revenez à Aion 2 après le lancement du test et laissez le jeu au premier plan.|اسلك المسار نفسه مرتين لمقارنة معدل الإطارات والإطارات البطيئة. عد إلى Aion 2 بعد بدء الاختبار وأبقِ اللعبة في المقدمة.
Set a limit to try steadier frame pacing. Close Aion 2 to apply it, then relaunch and measure again.|Teste ein Limit für gleichmäßigere Bildzeiten. Schließe Aion 2, übernimm das Limit, starte das Spiel erneut und miss noch einmal.|Essayez une limite pour améliorer la régularité des images. Fermez Aion 2 pour l’appliquer, puis relancez le jeu et mesurez à nouveau.|جرّب تحديد المعدل لتحسين انتظام الإطارات. أغلق Aion 2 لتطبيق الحد ثم أعد تشغيل اللعبة والقياس.
This changes only the FPS limit in Engine.ini. Visual effects are configured separately.|Dies ändert nur das FPS-Limit in Engine.ini. Visuelle Effekte werden separat eingestellt.|Seule la limite d’IPS dans Engine.ini est modifiée. Les effets visuels se règlent séparément.|يتغير حد الإطارات في Engine.ini فقط. تُضبط المؤثرات البصرية بشكل منفصل.
Measure for 60 seconds|60 Sekunden messen|Mesurer pendant 60 secondes|القياس لمدة 60 ثانية
Cancel performance test|Leistungstest abbrechen|Annuler le test|إلغاء اختبار الأداء
Apply FPS limit|FPS-Limit anwenden|Appliquer la limite d’IPS|تطبيق حد الإطارات
Restore previous FPS limit|Vorheriges FPS-Limit wiederherstellen|Rétablir la limite précédente|استعادة الحد السابق للإطارات
FPS limit updated. Relaunch Aion 2 to test it.|FPS-Limit geändert. Starte Aion 2 erneut, um es zu testen.|Limite mise à jour. Relancez Aion 2 pour la tester.|تم تحديث حد الإطارات. أعد تشغيل Aion 2 لتجربته.
Close Aion 2 to change the FPS limit.|Schließe Aion 2, um das FPS-Limit zu ändern.|Fermez Aion 2 pour modifier la limite d’IPS.|أغلق Aion 2 لتغيير حد الإطارات.
Engine.ini is read-only.|Engine.ini ist schreibgeschützt.|Engine.ini est en lecture seule.|ملف Engine.ini للقراءة فقط.
An existing FPS override uses a value outside this control.|Das vorhandene FPS-Limit liegt außerhalb des einstellbaren Bereichs.|La limite existante est hors de la plage de ce réglage.|حد الإطارات الحالي خارج نطاق هذا الإعداد.
Choose a limit from 20 to 500 FPS.|Wähle ein Limit zwischen 20 und 500 FPS.|Choisissez une limite entre 20 et 500 IPS.|اختر حدًا بين 20 و500 إطار في الثانية.
No FPS limit change is available to restore.|Es gibt keine Änderung des FPS-Limits zum Wiederherstellen.|Aucune modification de la limite à annuler.|لا يوجد تغيير في حد الإطارات يمكن استعادته.
FPS limit changed outside the Helper. Refresh before making another change.|Das FPS-Limit wurde extern geändert. Aktualisiere den Status vor weiteren Änderungen.|La limite a été modifiée en dehors du Helper. Actualisez avant de la modifier.|تغيّر حد الإطارات خارج Helper. حدّث الحالة قبل إجراء تغيير آخر.
The configuration backup record is invalid.|Der Eintrag der Konfigurationssicherung ist ungültig.|L’entrée de sauvegarde de la configuration est invalide.|سجل النسخة الاحتياطية للإعدادات غير صالح.
Another configuration change is in progress.|Eine andere Konfigurationsänderung läuft.|Une autre modification de configuration est en cours.|هناك تغيير آخر في الإعدادات قيد التنفيذ.
Return to Aion 2 to start the test.|Kehre zu Aion 2 zurück, um den Test zu starten.|Revenez à Aion 2 pour démarrer le test.|عد إلى Aion 2 لبدء الاختبار.
Measuring for 60 seconds. Keep Aion 2 in the foreground.|Messung für 60 Sekunden. Lass Aion 2 im Vordergrund.|Mesure pendant 60 secondes. Laissez Aion 2 au premier plan.|يستمر القياس 60 ثانية. أبقِ Aion 2 في المقدمة.
Performance test completed.|Leistungstest abgeschlossen.|Test de fluidité terminé.|اكتمل اختبار السلاسة.
Measuring FPS…|FPS werden gemessen…|Mesure des IPS…|جارٍ قياس معدل الإطارات…
You can return to the Helper|Du kannst zum Helper zurückkehren|Vous pouvez revenir au Helper|يمكنك العودة إلى Helper الآن
Performance test cancelled.|Leistungstest abgebrochen.|Test de fluidité annulé.|أُلغي اختبار السلاسة.
Test interrupted because Aion 2 lost focus.|Test unterbrochen, weil Aion 2 nicht mehr im Vordergrund war.|Test interrompu car Aion 2 a quitté le premier plan.|توقف الاختبار لأن Aion 2 خرج من المقدمة.
Not enough game frames to complete the test.|Nicht genug Spielbilder für einen vollständigen Test.|Nombre d’images insuffisant pour terminer le test.|عدد إطارات اللعبة غير كافٍ لإكمال الاختبار.
Previous test|Vorheriger Test|Test précédent|الاختبار السابق
Latest test|Letzter Test|Dernier test|الاختبار الأخير
{0} · {1:0.0} FPS average · p99 {2:0.0} ms · {3} frames over 50 ms|{0} · {1:0.0} FPS im Mittel · p99 {2:0.0} ms · {3} Bilder über 50 ms|{0} · {1:0.0} IPS en moyenne · p99 {2:0.0} ms · {3} images au-delà de 50 ms|{0} · متوسط {1:0.0} إطار/ث · p99 ‏{2:0.0} مللي ثانية · {3} إطارات تجاوزت 50 مللي ثانية
Same height|Auf gleicher Höhe|À la même hauteur|على الارتفاع نفسه
{0} m above|{0} m höher|{0} m plus haut|أعلى بـ {0} م
{0} m below|{0} m tiefer|{0} m plus bas|أسفل بـ {0} م
Relative to your movement|Relativ zur Bewegungsrichtung|Selon votre direction de déplacement|بالنسبة إلى اتجاه حركتك
Combat Log|Kampflog|Journal de combat|سجل القتال
Your recent hits in real time|Deine letzten Treffer in Echtzeit|Vos derniers coups en temps réel|ضرباتك الأخيرة في الوقت الفعلي
Enable combat log|Kampflog aktivieren|Activer le journal de combat|تفعيل سجل القتال
Auto-hide when inactive|Bei Inaktivität ausblenden|Masquer en cas d’inactivité|الإخفاء عند عدم النشاط
Hide after inactivity|Ausblenden nach Inaktivität|Masquer après une période d’inactivité|الإخفاء بعد عدم النشاط
Panel width|Panelbreite|Largeur du panneau|عرض اللوحة
Panel height|Panelhöhe|Hauteur du panneau|ارتفاع اللوحة
Line format|Zeilenformat|Format des lignes|تنسيق السطر
Icon, name and damage|Symbol, Name und Schaden|Icône, nom et dégâts|الأيقونة والاسم والضرر
Icon and damage|Symbol und Schaden|Icône et dégâts|الأيقونة والضرر
Name and damage|Name und Schaden|Nom et dégâts|الاسم والضرر
Show timestamps|Uhrzeit anzeigen|Afficher l’heure|إظهار الوقت
New entries at the bottom|Neue Einträge unten|Nouvelles entrées en bas|السجلات الجديدة في الأسفل
Use the bar on the right to review earlier hits. The log stays visible while you scroll.|Nutze die Leiste rechts, um frühere Treffer anzusehen. Das Protokoll bleibt beim Scrollen sichtbar.|Utilisez la barre à droite pour relire les coups précédents. Le journal reste visible pendant le défilement.|استخدم الشريط على اليمين لمراجعة الضربات السابقة. يظل السجل ظاهرًا أثناء التمرير.
Sample combat events|Beispieltreffer|Exemples de coups|أمثلة على الضربات
Shows your supported outgoing hits while Aion is in focus. Recent hits are kept for this session.|Zeigt unterstützte eigene Treffer bei aktivem Aion. Die letzten Treffer bleiben für diese Sitzung erhalten.|Affiche vos coups pris en charge lorsque Aion est au premier plan. Les coups récents sont conservés pour la session.|يعرض ضرباتك المدعومة عندما تكون نافذة Aion نشطة. تُحفظ الضربات الأخيرة لهذه الجلسة.
SAMPLE|VORSCHAU|APERÇU|معاينة
Waiting for your first hit|Warte auf deinen ersten Treffer|En attente de votre premier coup|في انتظار ضربتك الأولى
Show hit label|Trefferbezeichnung anzeigen|Afficher le type de coup|إظهار نوع الضربة
Hit label size|Schriftgröße der Trefferbezeichnung|Taille du texte du type de coup|حجم نص نوع الضربة
Hit label opacity|Deckkraft der Trefferbezeichnung|Opacité du type de coup|عتامة نوع الضربة
Hit label color|Farbe der Trefferbezeichnung|Couleur du type de coup|لون نوع الضربة
Hit label outline color|Konturfarbe der Trefferbezeichnung|Couleur du contour du type de coup|لون حدود نوع الضربة
Hit label outline thickness|Konturstärke der Trefferbezeichnung|Épaisseur du contour du type de coup|سُمك حدود نوع الضربة
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
Animate key presses and skill casts|Tastendruck und Fertigkeitseinsatz animieren|Animer les appuis et les lancements|تحريك الخانات عند ضغط المفاتيح وإطلاق المهارات
Use custom labels such as Q, 1, F1 or M1 to match your AION 2 bindings. Confirmed casts flash as well, including skills used by game macros. AION 2 must be in front and widgets locked.|Verwende Beschriftungen wie Q, 1, F1 oder M1 passend zur Tastenbelegung. Bestätigte Einsätze blinken auch bei Spielmakros. AION 2 muss im Vordergrund sein und die Widgets müssen gesperrt sein.|Utilisez des étiquettes comme Q, 1, F1 ou M1 pour vos raccourcis. Les lancements confirmés clignotent aussi via une macro du jeu. AION 2 doit être au premier plan et les widgets verrouillés.|استخدم تسميات مثل Q أو 1 أو F1 أو M1 لمطابقة مفاتيح اللعبة. يضيء الإطار عند تأكيد إطلاق المهارة حتى عند استخدام ماكرو اللعبة. يجب أن تكون AION 2 في المقدمة وعناصر الواجهة مقفلة.
The test shows a blocked skill in slot 2 and an active reactive skill in slot 3.|Im Test zeigt Platz 2 eine gesperrte und Platz 3 eine verfügbare reaktive Fähigkeit.|Le test affiche une compétence bloquée dans le deuxième emplacement et une compétence réactive disponible dans le troisième.|يعرض الاختبار مهارة غير متاحة في الخانة الثانية ومهارة تفاعلية متاحة في الخانة الثالثة.
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
