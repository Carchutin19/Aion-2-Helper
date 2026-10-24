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

La copia del HUD del juego se prepara por separado; no cambia al pasar entre los
preajustes de personaje del Helper.

Los contadores usan el CD recibido del juego y sus correcciones posteriores.
Si no se ha recibido un CD, no se inventa uno. La vista previa es una simulación.
Las habilidades con cargas y otras condiciones especiales requieren validación
específica; no todas las variantes están confirmadas.

## Ocultar solo las barras originales

Puedes hacerlo **con AION 2 abierto**, sin reiniciar el juego. Se conserva la
misma distribución del HUD elegido: chat, minimapa, barras de vida y el resto de
sus ajustes. La copia cambia únicamente la visibilidad de las tres barras de
habilidades originales.

1. En AION 2, abre **Editar HUD** con tu distribución actual y **expórtala**.
   Ponerle un nombre es opcional, pero facilita encontrar el archivo.
2. En **Helper → Hotbar**, pulsa **Crear HUD sin hotbar** y elige ese archivo
   exportado `.dat`. No elijas `DeviceSetting_V2.dat`.
3. Se crea una copia en la misma carpeta:
   `Helper_HiddenHotbar(nombre original).dat`. **Mostrar archivo creado** permite
   localizarla. El archivo original queda intacto; si ya existe una copia distinta
   con ese nombre, se crea otra numerada en lugar de sustituirla.
4. Vuelve a **AION 2 → Editar HUD → Importar archivo** y elige la copia
   `Helper_HiddenHotbar`. Importa en un preajuste distinto para conservar también
   tu HUD original disponible dentro del juego.
5. **Selecciona el preajuste importado, pulsa Guardar y sal del editor de HUD.**
   Las barras pueden verse mientras editas el HUD. Comprueba que se ocultan una
   vez guardado y fuera del editor.

**Crear el archivo no lo activa en el juego.** El Helper indica que la copia está
preparada; la importación, selección y guardado se hacen en AION 2.

Para recuperar las barras originales, selecciona tu preajuste original o importa
el archivo exportado original, **guarda y sal del editor**. Si después cambias tu
distribución, exporta el HUD actualizado y prepara una nueva copia.

Este flujo sustituye la edición automática de `DeviceSetting_V2.dat`: no espera
al cierre del juego ni reescribe sus ajustes en segundo plano. Los registros y
copias de seguridad del método anterior se conservan, pero sus antiguas opciones
no activan escrituras al arrancar esta versión.
