   M S B A M L   `   `   `  ����   BSAutoGenerator     BSAutoGenerator.MainWindow   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Aav2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     -     �Name$  Window�� � Title$ BSAutoGenerator��$	��205��$
��Auto��
����·� � WindowStyle$ 
ToolWindow=� � 
ResizeMode$ NoResize=� � SizeToContent$ WidthAndHeight=� � WindowStartupLocation$ CenterScreen=�.��� .+��� -    ��Name$ 
WindowSize��$��Center=�$	��300��$	��205��
���   �$��	0,-16,0,0q��� -    ��Name$ 
OpenButton�� � ToolTip$ Open info DAT file��$��	Open File��$
��Left=�$	��Top=�$	��200��$	��150��$��Arial)�$��20(�$y�	Collapsed=��� -   $ 	OpenAudio��$* $Open MP3 or WAV file for Automapping��$��
Open Audio��$
��Left=�$	��Top=�$	��200��$	��150��$��Arial)�$��20(�$y�	Collapsed=�$��	150,0,0,0q�� $��300,0,-150,0q�.+��� -   	 ��Name$	 DiffListBox��$
��Left=�$	��190��$	��190��$	��150��$	��150��$y�	Collapsed=�
 �� SelectionMode$
 Single=�&   System.Windows.Controls.DataGrid  -     Name$ DiffDataGrid��$	��225��$	��150��$y�	Collapsed=�${�False���� -   $ LightButton��$0 *Generate a light show using note as timing��$��Light��$
��Left=�$	��Top=�$	��150��$��20(�$��Arial)�$y�	Collapsed=�$��38���� -   $ DownLightButton��$L FReduce a light show strobe, spin/zoom and turn on light during silence��$��	Downlight��$
��Left=�$	��Top=�$	��150��$��20(�$��Arial)�$y�	Collapsed=�$��	150,0,0,0q�$��38���� -	   $ DDButton��$+ %Convert a map into Double Directional��$��DD��$
��Left=�$��Center=�$	��150��$��20(�$��Arial)�$y�	Collapsed=�$��	150,0,0,0q�$��38���� -
   $ ChainButton��$1 +Convert a map into chains (the new sliders)��$��Chain��$
��Left=�$	��Top=�$	��150��$��20(�$��Arial)�$y�	Collapsed=�$��
150,38,0,0q�$��38���� -   $ InvertButton��$1 +Convert a map into a basic inverted version��$��Inverted��$
��Left=�$	��Top=�$	��150��$��20(�$��Arial)�$y�	Collapsed=�$��	0,113,0,0q�$��38���� -   $ 
BombButton��$# Add random bombs around notes��$
��Bomb��$
��Left=�$��Center=�$	��150��$��20(�$��Arial)�$y�	Collapsed=�$��38���� -   $ LoloppeButton��$  Add Loloppe notes to notes��$��Loloppe��$
��Left=�$	��Top=�$	��150��$��20(�$��Arial)�$y�	Collapsed=�$��150,113,0,0q�$��38���� -   $ AutomapperButton��$( "Convert a map into a brand new map��$��
Automapper��$
��Left=�$	��Top=�$	��150��$��20(�$��Arial)�$y�	Collapsed=�$��	0,151,0,0q�$��38���� -   $ 	ArcButton��$ Add arc between notes��$	��Arc��$
��Left=�$	��Top=�$	��150��$��20(�$��Arial)�$y�	Collapsed=�$��0,38,0,0q�$��38���� -   $ 
SaveButton��$6 0Generate the new files and close BSAutoGenerator��$
��Save��$
��Left=�$	��Top=�$	��150��$��20(�$��Arial)�$y�	Collapsed=�$��150,151,0,0q�$��38��