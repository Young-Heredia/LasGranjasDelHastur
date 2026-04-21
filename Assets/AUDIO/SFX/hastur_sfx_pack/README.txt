Paquete básico de efectos de sonido para 'Las Granjas del Hastur'

Coloca aquí los archivos .wav del zip (intro_open, intro_panel_change, etc.).

Scripts del proyecto (no uses una copia duplicada en esta carpeta):
- Assets/Scripts/AudioManager.cs — objeto vacío "AudioManager" en IntroComic y MainMenu; asigna música y todos los clips SFX.
- Assets/Scripts/BasicUIAudio.cs — ya añadido a los botones; arrastra ui_hover, ui_click o ui_back según el botón.

En el Inspector del AudioManager:
- introOpen, introPanelChange, introPanelChangeAlt, narrationTextBlip
- uiHover, uiClick, uiBack
- introMusic (opcional), mainMenuMusic (recomendado para el menú)

En cada BasicUIAudio de botón:
- hoverClip → ui_hover.wav
- clickClip → ui_click.wav en Siguiente / Jugar / Opciones; ui_back.wav en Saltar / Salir

Paneles con sonido "alternativo" (contrato, última viñeta): índices 3 y 5 en altPanelIndices (editable en AudioManager).

Archivos esperados del zip:
- ui_hover.wav, ui_click.wav, ui_back.wav
- intro_open.wav, intro_panel_change.wav, intro_panel_change_alt.wav
- narration_text_blip.wav

Importación recomendada en Unity:
- Load Type: Decompress On Load
- Compression Format: PCM o Vorbis
- Sample Rate Setting: Preserve Sample Rate
