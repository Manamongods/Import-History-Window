# Import History Window for Unity
 
This is a simple and very small window that can be used to quickly get access to recently imported (modified/created) files, instead of traversing your folder hierarchies.

This is especially useful because you've probably already traversed the folders in whatever external programs you used, to create the files, and it's a pain to have to find the locations again, in Unity.

![IHW](https://user-images.githubusercontent.com/45214197/83461368-d37d9c00-a468-11ea-9764-6c1311eb808b.PNG)

Whenever a file is modified repeatedly, the older entries are removed.

- You add the window in "Window/Import History"
- Double clicking will open the file
- There is a small button beside each entry, which opens the containing folder in File Explorer.
- You can clear the list by clicking the 3 vertical dots at the top right corner of the window, and clicking "Clear". There is not much reason to do this however.


You have the following settings which can be modified in the .cs file:
- comment out "#define FOLDER_BUTTON" at the top, if you don't want the button which opens the folder
- You can add/remove extensions in IGNORED_EXTENSIONS. These extensions are for files that do not communicate directly with Unity, such as PixaFlux's .pxf or Affinity's .afdesign. Those files create other files which ARE read by Unity, but aren't useful within Unity themselves, so it's best to ignore those extensions.
- HISTORY_LENGTH is the maximum number of entries remembered
- DOUBLE_CLICK_TIME is the maximum length of time that can register a double click (to open the file)
- HEIGHT is the height of an entry
- MARGIN is the spacing (left, right, top, and bottom) between the buttons
