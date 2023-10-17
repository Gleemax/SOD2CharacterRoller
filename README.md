# SOD2CharacterRoller
A self-made sod2 character roller using tesseract ocr engine.

Recently Version: 1.2.1
UI Language Support: Chinese
Gameplay Lanaguage Support: English

How to use:
1. Open software and state of decays 2, the gameplay language should be english.
2. Press "自动校准" (means auto calibration) button, the software will automaticlly calculate the coordinates of 3 character rects.
3. Switch your game interface to a character roll page, don't let the roller ui cover any rect of traits.
4. Check the "测试模式" (means test mode), then press keypad 0 or “启动” (means startup) button, the ocr result will display in the left side.
5. If the ocr result is correct, uncheck the "测试模式" button. If it's not, considering about set a higher resolution for your game and redo step 4.
6. Fill the target weight on the right as you wish, then press keypad 0 or "启动" button to start the roll process.
7. The roll process will end when target weight is met, or when you press the delete or keypad 0.

Calculation of weight:
  The weight of a trait depend on the trait list on the left side. 
  The weight of a character is equal to the sum of all traits he have.
  When the weight of the character is bigger than the target weight you fill, the roll process of that slot will stop.

Example of a weight calculation:
  If weight of "blood plague survivor" is set to 5, and weight of "unbreakable" is set to 4.
  And target weight is set to 8.
  Any slot could stop when a character with these 2 traits occur.
  Because the total weight of that character is 4+5=9 > 8.

Notice:
1. Due to ocr effectiveness and speed, this software is designed for a english gameplay language.
2. Recently this software is only support chinese language, a english UI language version is considering.
3. The link-label recently built in is using to join a chat group, or open a chinese sod2 wiki, please ignore if you're not a chinese.
