# Hotbar

La hotbar es la barra de habilidades. El apartado **Hotbar** del Helper permite
mostrar una barra propia y ocultar, por separado, las barras originales de AION 2.

## Preparar la barra del Helper

1. Activa **Mostrar hotbar del Helper** antes de entrar con tu personaje.
2. Desbloquea los widgets en **General** para colocarlos.
3. **Todo junto** mueve y escala la barra completa. Desactívalo para ajustar un
   slot por separado o marcar **Ocultar este slot**.
4. El **snap** alinea slots cercanos por cualquier lado. **Separación entre slots**
   establece el hueco al ajustar u ordenar. Puedes desactivar el snap.

Solo se muestran habilidades asignadas detectadas; se omiten objetos y huecos
vacíos. Los números de los slots son etiquetas consecutivas del Helper, no teclas
detectadas. Puedes sustituir cada etiqueta por la tecla que usas.
Si borras una etiqueta personalizada, vuelve a aparecer su número automático.

**Probar hotbar** muestra CD simulados que bajan, terminan y vuelven a empezar.
El mismo botón pasa a llamarse **Detener prueba**. La prueba continúa hasta que
la pares; no cambia los CD recibidos ni activa permanentemente la hotbar.

## Un preajuste por personaje

El encabezado muestra el personaje y su servidor. Al detectar tu entrada, el
Helper carga su preajuste y guarda automáticamente los cambios: posición,
escala, aspecto, etiquetas y slots ocultos. Se usa nombre y servidor, como en
Mascotas; el ID temporal de la sesión no es la clave del preajuste.

La primera vez se conserva tu distribución actual para el primer personaje
identificado. Los personajes posteriores empiezan sin copiar las etiquetas,
slots ocultos o habilidades guardadas de otro personaje. Al volver al anterior,
se recupera su configuración.

Al abrir el Helper sin estar dentro del juego, el último perfil se identifica
como **guardado**, pendiente de confirmar la entrada. Si todavía no se ha
detectado ninguno, se indica **Sin personaje detectado**. Los datos se conservan
en `hotbar-profiles.json`; un archivo incompatible no se sobrescribe.

La opción **Ocultar hotbar de AION 2** es global y no cambia al pasar de personaje.

Los contadores usan el CD recibido del juego y sus correcciones posteriores.
Si no se ha recibido un CD, no se inventa uno. La vista previa es una simulación.
Las habilidades con cargas y otras condiciones especiales requieren validación
específica; no todas las variantes están confirmadas.

## Ocultar las barras originales

Marca **Ocultar hotbar de AION 2**. No necesitas exportar ni importar tu HUD.

Si el juego está abierto, el cambio queda pendiente. Cierra AION 2 y deja el
Helper abierto. Se guardará una copia y se aplicará el cambio antes de que vuelvas
a abrir el juego. Se cambian únicamente las tres barras originales de habilidades
en los preajustes de HUD guardados; se conserva el resto de la distribución.

Para restaurarlas, desmarca la opción y repite el mismo proceso con el juego
cerrado. Se restaura la visibilidad anterior de cada barra, conservando otros
cambios que hayas hecho después en el HUD. Cerrar el Helper por sí solo no
restaura ni modifica el HUD del juego.

El estado indica si el cambio está pendiente o aplicado. Si no se encuentra la
configuración, usa **Volver a comprobar**. Con varias instalaciones puede ser
necesario **Localizar configuración del HUD** y seleccionar su archivo
`DeviceSetting_V2.dat`. Los formatos desconocidos se rechazan sin modificarlos.

Las copias se conservan en `%LOCALAPPDATA%\Aion2Helper\HotbarBackups`.
No se modifica el HUD hasta que se usa expresamente esta opción.
