   M S B A M L   `   `   `  ���� U  QMahApps.Metro, Version=0.13.1.0, Culture=neutral, PublicKeyToken=f4fb5a3c4d1e5b4fV;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.MetroMahApps.Metro.Controls   	MrBootman   MrBootman.MainWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Pcontrols;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro     7Custom*http://metro.mahapps.com/winfx/xaml/shared   -   (   "MahApps.Metro.Controls.MetroWindow   Name$  frmMainWindow�� � Title$ Mr. Bootman��$	��514��$	��599�� � WindowStartupLocation$ CenterScreen=� � Icon$ boot-man.ico�� � ToolTip$9 3Creates and extracts images to and from disk drives��|�.   � 	Resources    ButtonStyle1 �� 
TargetType �� Property �� Value  UpperCaseContentFontSize  DefaultFont  GrayBrush10  TextBoxBorderBrush  	TextBrush	 �� 
TargetType'  !System.Windows.VisualStateManager
   VisualStateGroups%  System.Windows.VisualStateGroup   States   System.Windows.VisualState   
Storyboard �� TargetProperty �� 
TargetName9  3System.Windows.Media.Animation.EasingDoubleKeyFrame O� KeyTime O� Value ��Name �� CornerRadius ��Name  ControlsDisabledBrush J� IsHitTestVisible J� Opacity �� RadiusY �� RadiusX  
GrayBrush8  
GrayBrush7 #	 ButtonMouseOverInnerBorderBrush 
 ButtonMouseOverBorderBrush ��Name �� 	Converter/   )MahApps.Metro.Converters.ToUpperConverter��%f  &	        �� " ��.���  � ��$ 25����  � ��$
 Bold"���  � ��# �  ��  � ��# �  ��  � ��# �  ��  � ��# �  ��  � ��# �  ��  � ��$	 5,6q���  � ��$ 1q���  � �� �� "	 ��.R�� 
   CommonStates��.   
Normal��  	MouseOver��. �� .��U� $ (UIElement.Opacity)�$ MouseOverBorder��.\�  $ 0��$ 1Q�  Pressed��. �� .��U� $ (UIElement.Opacity)�$ PressedBorder��.\�  $ 0��$ 1Q�  Disabled��. �� .��U� $ Opacity�$ DisabledVisualElement��.\��� $ 0��$	 0.7Q�U� $ (UIElement.Opacity)�$ contentPresenter��.\�  $ 0��$	 0.3Q�  FocusStates��.   Focused��. �� .��U� $ (UIElement.Opacity)�$ FocusRectangle��.\�  $ 0��$ 1Q�U� $ (UIElement.Opacity)�$ FocusInnerRectangle��.\�  $ 0��$ 1Q�  	Unfocused��.+��� $ 
Background��#��z��#��z��#��z��$ 3���� $ DisabledVisualElement��#���   .  $ 0Q�$ 3��$ 3���� $ MouseOverBorder��#��z��#���  $	 3.5��$ 0Q��� $ PressedBorder��#��z��#���  $	 3.5��$ 0Q��� $ FocusRectangle��$��-1q�$ 0Q�$ 4��$ 4��#��� 	 �� $ FocusInnerRectangle��$ 0Q�$ 3��$ 3��#��� 
 #��z���� $ contentPresenter��#��z��#��z��#��z����. #��z������ *	Content+   .��� -    �Name$ grdMainGrid�� � ColumnDefinitions �� $	��67* ��� $
��505* �����  �� EndPoint$ 0.5,1&� � MappingMode$ RelativeToBoundingBox=� �� 
StartPoint$ 0.5,0&�.���  � Color$ 	#FFDED9C3��  � Offset$  0.245Q�� $ White��! � RowDefinitions! �� $
��Auto ��� $	��145 ��� $��35 ��� �� $
��Auto �.+��� -   " ��Name$" tabctrlBootman��$��1��$��	5,5,5,-33q�$��2����Z� .@��� -   # ��Name$# tabitemImageToDrive��$��Image To Drive��+   %MahApps.Metro.Controls.ControlsHelper$   HeaderFontSize$$ 22Q�%   HeaderFontWeight$% SemiBold"�$ From image file to drive����Z� .��� $��1��$��3��$��	0,0,0,-33q����� & �� Color$& 
WhiteSmoke��' �� Opacity$	' 0.6Q� �� $	��110 ��� $��65 ��� $
��330* ��� $��80 �! �� $��45 ��� $��45 ��� $��* �.+��� $��Destination Drive:��$��0��$
��Left=�$��5,9,0,10q�$��1��$��Center=�$* $Where the contents will be extracted���� $��Image File:��$��0��$
��Left=�$��	25,10,0,9q�$��0��$��Center=�$+ %The file to be extracted to the drive���� -   ( ��Name$( txtTab1ImageFile��$��Stretch=�$��0��$��Center=�$��3��$��100,10,10,9q�) �� 
IsReadOnly) . * �� 
CaretBrush* �	#FFA67C22���	#FF814509$ Path of image file��.������ $ 0.5,1&�$ 0.5,0&�.��� $ 	#FFE6E4E4��$  0Q�� $ 
WhiteSmoke��$  0.817Q�.���� -   + ��Name$+ btnTab1Browse��$��Browse��$��3��$
��Left=�$��0,10,0,9q�$��0��$��Center=�$��54��$��26��#���   $- 'Select the location of the source image������ $ 0.5,1&�$ 0.5,0&�.��� $ 	LightGray��$  0Q�� $ White��$  1Q��� -   , ��Name$#, cboTab1DestinationDriveLetter��$��1��$��1��$
��Left=�$��Center=�$��54��$��0,9,0,10q�$ select a drive here������ $ 0.5,1&�$ 0.5,0&�.��� $ 	#FFF3F3F3��$  0Q�� $ 	#FFDADADA��$  1Q��� -   - ��Name$&-  chkTab1ExtractImageDirectToDrive��$#��Extract Image Direct to Drive��$��right=�$��1��$��2��$	��Top=�$��5,2,0,0q�$��1��$/ )Bypass extracting image to computer first���� -	   $- chkTab1FormatBootReady��$��Format/Make Boot Ready��$��Right=�$��1��$��2��$	��Top=�$��	4,25,17,0q�$��1��$+ %Make this iso bootable when extracted��. d� 	IsChecked$
. TrueY��� -
   $# tabitemDriveToDrive��$��Drive To Drive��$$ 22Q�$ Clone an existing drive��$% SemiBold"���Z� .��� $��1��$��3��$��0q����� $& 
WhiteSmoke��$	' 0.6Q� �� $	��110 ��� $��65 ��� $
��139* ��� $	��62* �! �� $��45 ��� $��45 ��� $��* �.+��� $��Destination Drive:��$��0��$
��Left=�$��5,9,0,10q�$��1��$��Center=�$&  The drive to copy the source too���� $��Source Drive:��$��0��$
��Left=�$��	29,10,0,9q�$��0��$��Center=�$ The drive to copy from���� -   $, cboTab2SourceDriveLetter��$��1��$��0��$
��Left=�$��Center=�$��60��$��0,10,0,9q����� $ 0.5,1&�$ 0.5,0&�.��� $ 	#FFF3F3F3��$  0Q�� $ 	#FFDADADA��$  1Q��� -   $- chkTab2FormatBootReady��$��Format/Make Boot Ready��$��Right=�$��0��$��2��$��	0,0,30,22q�$��2��$ Make flash drive bootable��$
. TrueY��� -   $- chkTab2UseXCopy��$��Use XCopy for File Copy��$��Right=�$��0��$��2��$	��Top=�$��	0,25,38,0q�$��2��$' !Copy directly from flash to flash���� -   $#, cboTab2DestinationDriveLetter��$��1��$��1��$
��Left=�$��Center=�$��60��$��0,9,0,10q����� $ 0.5,1&�$ 0.5,0&�.��� $ 	#FFF3F3F3��$  0Q�� $ 	#FFDADADA��$  1Q��� -   $# tabitemCreateImage��$��Create Image��$$ 22Q�$% SemiBold"�.��� $��1��$��3������ $& 
WhiteSmoke��$	' 0.6Q� �� $	��110 ��� $
��395* ��� $��80 �! �� $��45 ��� $��45 ��� $��* �.+��� $��Destination File:��$��0��$
��Left=�$��5,0,0,0q�$��1��$��Center=�$* $Where the image file will be created���� $��Source Directory:��$��0��$
��Left=�$��4,10,0,9q�$��0��$��Center=�$) #The source to create the image from���� -   $( txtTab3SourceDirectory��$��Stretch=�$��0��$��Center=�) . $��2��$��
107,10,3,9q�.������ $ 0.5,1&�$ 0.5,0&�.��� $ 	#FFE6E4E4��$  0Q�� $ 
WhiteSmoke��$  0.815Q�.���� -   $+ btnTab3BrowseSource��$��Browse��$��2��$
��Left=�$��	2,10,0,10q�$��0��$��Center=�$��53��$! browse the source directory������ $ 0.5,1&�$ 0.5,0&�.��� $ 	LightGray��$  0Q�� $ White��$  1Q��� -   $( txtTab3DestinationFile��$��Stretch=�$��1��$��Center=�) . $��2��$��
107,9,3,10q�.������ $ 0.5,1&�$ 0.5,0&�.��� $ 	#FFE6E4E4��$  0Q�� $ 
WhiteSmoke��$  0.815Q�.���� -   $+ btnTab3BrowseDestination��$��Browse��$��2��$
��Left=�$��	2,10,0,10q�$��1��$��Center=�$��53��$&  browse the destination directory������ $ 0.5,1&�$ 0.5,0&�.��� $ 	LightGray��$  0Q�� $ White��$  1Q��� -   $# tabitemAbout��$��About��$$ 22Q�$ About Bootman��$% SemiBold"�.��� -   $ 	Info_Grid������ $& 
WhiteSmoke��$	' 0.6Q� �� $	��100 ��� $	��375 ��� $��* �! �� $��30 ��� $��18 ��� $��18 ��� $��* �.+��� $��Mr. Bootman��$��0��$
��Left=�$��Center=�$��15,6,0,6q�$��24(�$��Microsoft Sans Serif)�$��2��$��38��$	��178���� -   / ��Name$/ 
lblVersion��$��Version 1.0��$
��Left=�$��Bottom=�$��	15,0,0,15q�$��12(�$��Microsoft Sans Serif)�$��24��$��3��$ The program version���� $��© 2014 Fusion Fjord LLC��$��2��$
��Left=�$��Center=�$��2��$��15,3,0,3q�$��12(�$��Microsoft Sans Serif)�$��24��$	��169���� -   $+ btnTab4SysInfo��$��System Info��$
��Left=�$��2��$��1��$��Center=�$��80��$��28��$��
362,2,0,52q�$) #Info about your computer and drives��$��2��$��2��0 J� SnapsToDevicePixels0 .  ���� $ 0.5,1&�$ 0.5,0&�.��� $ 	LightGray��$  0Q�� $ White��$  1Q��� $��.��$��0��$
��Left=�$��Center=�$��
75,-2,0,10q�$��26(�$��Microsoft Sans Serif)�$��2��$��38��$	��178���� $��.��$��0��$
��Left=�$��Center=�$��
88,-2,0,10q�$��26(�$��Microsoft Sans Serif)�$��2��$��38��$	��178���� $��1��$
��Left=�$	��100��$��
112,10,0,0q�$	��Top=�$	��100��$��4��$��boot-man.ico��� $��1��$��	7,143,7,5q�$��2��$��2������ $ 0.5,1&�$ 0.5,0&�$' 0Q�.��� $ 	#FFB9B9B9��$  1Q�� $ 	#FFE6E6E6��$  0.251Q� �� $	��120 ��� $��40 ��� $	��180 ��� ! �� .+��� -   $+ 	btnCreate��$��Create��$��Right=�$��0,3,20,3q�$��Center=�$��70��$��26��#���   $0 *Prepare the drive from the selected source��1 �� 	IsDefault1 . ���� $ 0.5,1&�$ 0.5,0&�.��� $ 	LightGray��$  0Q�� $ White��$  1Q��� -   $+ 
btnRefresh��$��0,0,0,0q�$��25��$+ %reset selection and rescan for drives��$��1����Z� ���� .R��� $��Center=�$��Center=�$��2q���Z� ._��� $��Images/view-refresh.tif��$��22��$��22��2 J� OpacityMask2 �� 3 �� ImageSource$3 Images/view-refresh.tif��4 j� Stretch$4 UniformToFill=��� -   $/ lblStatusLabel��$��Status:��$��0��$��2��$
��Left=�$��Center=�$��0,0,0,0q�$��14(�$��Microsoft Sans Serif)�$��30��$
��Bold"�$ The stage of creation���� -   $/ lblStatusDisplay��$�� ��$��0��$��2��$��Stretch=�$��Center=�$��50,0,0,0q�$��14(�$��Microsoft Sans Serif)�$��30��-   'MahApps.Metro.Controls.MetroProgressBar  -   5  Name$5 progressBarBootman��$��0��$��3��$��5,5,10,5q�$��20�����	#FF876200��Z� ��Z� �� -   $( txtLogOutput��$�� ��$��3��$��Stretch=�"6 �� VerticalScrollBarVisibility$
6 Auto=�$��2��$��5,0,5,0q�7 �� TextWrapping$7 WrapWithOverflow=�8 J� 	AllowDrop8 .  .������ $ 0.5,1&�$ 0.5,0&�.��� $ 	#FFE6E6E6��$  0Q�� $ White��$
  0.39Q�.��� $��4��$��Bottom=�$��2�� �� $��60 ��� $��* �! �� .+��� -   9 ��Name$
9 SBar��$��2��.@��� .���� -   $- 	chkBtnLog��$��Show Log��$
. TrueY�$# Show progress log and details���� $��0,0,5,0q�$��Right=�.���� -    $+ btnClearLog��$��Clear��$��20��$ Clear the log screen������ $ 0.5,1&�$ 0.5,0&�.��� $ 	LightGray��$  0Q�� $ White��$  1Q��� $
��Left=�$��67��$��	26,10,0,0q�$��1��$	��Top=�$	��537��$��2��: K� ZIndex$: -1����
� .��� $ 	#FFC1BDBD��$  0.307Q�� $ 	#FFE6E6E6��$  0.616Q�