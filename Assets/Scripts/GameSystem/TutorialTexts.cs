/// <summary>
/// Textos del tutorial. Estan separados de TutorialManager para que, cuando se
/// agregue el ingles, solo haya que cambiar este archivo (por ejemplo, devolver
/// uno u otro idioma segun una preferencia).
///
/// Nota: la fuente del juego (Pixellari SDF) es estatica y solo trae ASCII. Si un
/// texto tiene acentos o signos de apertura, TutorialManager los limpia solo en
/// runtime cuando la fuente no los soporta.
/// </summary>
public static class TutorialTexts
{
    public const string Next = "Siguiente";
    public const string Skip = "Saltar";
    public const string Start = "¡Vamos!";
    public const string Finish = "¡A jugar!";
    public const string HowToPlayButton = "Cómo jugar";

    public const string Welcome =
        "¡Bienvenido, vaquero!\nTe enseñaré lo básico para que empieces a ganar oro.";
    public const string WelcomeReplay =
        "¡Repasemos lo básico, vaquero!";

    public static string Lanes(int done, int total) =>
        $"Desliza hacia los lados, o toca un carril, para mover a tu vaquero.\n({done}/{total})";

    public const string Kill =
        "Tu vaquero ataca solo a todo lo que se cruce en su camino.\n¡Ponte frente a un enemigo!";

    public const string KillMore =
        "¡Muy bien! Cada enemigo que derrotas suelta oro.";

    public static string Gold(string cost, string current) =>
        $"Junta {cost} de oro para comprar tu primera mejora.\nOro: {current} / {cost}";

    public const string GoldInfo =
        "El oro lo consigues derrotando enemigos y, con mejoras, también por segundo.";

    public const string OpenMenu =
        "¡Ya tienes suficiente!\nToca aquí para abrir el menú de mejoras.";

    public static string Buy(string upgradeName) =>
        $"Toca para comprar {upgradeName}.\nLas mejoras te dan oro por segundo, ¡incluso cuando no estás jugando!";

    public const string UpgradesInfo =
        "En el menú de mejoras gastas tu oro para ganar más oro por segundo. Cada mejora es más cara que la anterior.";

    public const string CloseMenu =
        "¡Excelente! Ahora cierra el menú para volver a la acción.";

    public const string Horse =
        "Este es el caballo. Tócalo para activar su carga: todo va más rápido y llueven enemigos... ¡y oro!";

    public const string HorseDone =
        "¡Yiiija! Mientras dura la carga, cada enemigo cuenta. Luego el caballo necesita descansar un rato.";

    public const string HorseInfo =
        "El botón del caballo activa una carga: todo va más rápido y llueven enemigos y oro. Después necesita descansar.";

    public const string Understood = "Entendido";

    public const string BonusIntro =
        "De vez en cuando cae un objeto brillante: es un bonus.\nPásale por encima para atraparlo. ¡Veamos uno!";

    public const string BonusCatch =
        "¡Atrápalo antes de que se escape!";

    public const string BonusMissed =
        "¡Se escapó! Inténtalo otra vez.";

    public const string BonusDone =
        "¡Bonus activado! Dura un rato, aprovéchalo.";

    public const string BonusDoneReplay =
        "¡Así se atrapa un bonus!";

    public const string BossIntro =
        "Cada cierto tiempo aparece un jefe.\nVamos a practicar con uno de prueba.";

    public const string BossInfo =
        "Cada cierto tiempo aparece un jefe. Mantén la pantalla presionada para disparar más rápido.";

    public const string BossFight =
        "Tu vaquero dispara solo. ¡Mantén la pantalla presionada para disparar más rápido y derrótalo!";

    public const string BossDone =
        "¡Jefe derrotado! Los jefes de verdad dan mucho oro y te llevan a una nueva zona.";

    public const string End =
        "¡Eso es todo, vaquero!\nPuedes repasar esto cuando quieras desde la pestaña de ajustes del menú de mejoras.";

    public const string EndReplay =
        "¡Listo! Puedes volver aquí cuando quieras.";
}
