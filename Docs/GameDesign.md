# LAST STOP — MVP design baseline
Genre: 2D bus survival roguelike, Unity 6, C#.
EXTERIOR: top-down view; player survives waves using automatic attacks. Bus remains a central gameplay object.
INTERIOR: side-view manual combat; enemies breach bus while exterior simulation continues.
Player can switch views; interior breach counter reaches warning threshold at 50; player must deal with threats and seal entry points; then return exterior.
HP <= 0 triggers game over. Other defeat conditions, precise controls, progression, enemies, art direction and numerical balance are TBD and require director approval.
First playable: one exterior arena, one bus interior, one enemy archetype, auto attack, manual interior attack, breach counter, scene/view switch, one seal interaction, health and game over, placeholder visuals/audio.
Technical decision pending: single scene with dual camera/areas vs additive scenes. Development proposes before implementing.
