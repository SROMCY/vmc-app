   M S B A M L   `   `   `  ����>  :VMC, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullclr-namespace:VMCVMC  5   	       VMC.OffsetDialog   5   	   e aSystem.Windows.Controls.Ribbon, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   #localclr-namespace:VMC     5   	     � Title$  Offset Dialog��5   	    � Icon$ .\Icons\Settings.png��6    � SizeToContent$ Manual=�6;    � WindowStartupLocation$ CenterOwner=�6R   $	��350��5	   	   $	��300��6   $	��300��6"   $	��250��61    � 
ResizeMode$ CanResizeWithGrip=�6A    � ShowInTaskbar .   � Topmost . .��5
      � $��15q�6    � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
   � $��0��6   $��0��6    5      �� 5      $
��Auto �6#   �� 5      $
��Auto �6#   �� 5      $��60 �6#    5      �� 5      $
��Auto �6    �� 5      $
��Auto �6    �� 5      $
��Auto �6    �� 5      $
��Auto �6    .+�5!      �� $��0��6   $��0��6%   $��Center=�65   $��Center=�6P   X:6q   �� 5"      -   5        	 ��Name$	 
TxtOffsetX��5"      $��0��6*   $��1��67   $��Center=�6G   $��Right=�6b   $��70��6~   $��Right=�6�   .��6�   0.006�   �� 5#      $��0��6   $��2��6%   $��Center=�65   $��Right=�6P   -28 mm6t   �� 5$      $��1��6   $��0��6%   $��Center=�65   $��Center=�6P   Y:6q   �� 5%      -   5        $	 
TxtOffsetY��5%      $��1��6*   $��1��67   $��Center=�6G   $��Right=�6b   $��70��6~   $��Right=�6�   .��6�   0.006�   �� 5&      $��1��6   $��2��6%   $��Center=�65   $��Right=�6P   -1 mm6s   �� 5'      $��2��6   $��0��6%   $��Center=�65   $��Center=�6P   Z:6q   �� 5(      -   5        $	 
TxtOffsetZ��5(      $��2��6*   $��1��67   $��Center=�6G   $��Right=�6b   $��70��6~   $��Right=�6�   .��6�   0.006�   �� 5)      $��2��6   $��2��6%   $��Center=�65   $��Right=�6P   5 mm6r   �� 5*      $��3��6   $��0��6%   $��Center=�65   $��Center=�6P   T:6q   �� 5+      -   5        $	 
TxtOffsetT��5+      $��3��6*   $��1��67   $��Center=�6G   $��Right=�6b   $��70��6~   $��Right=�6�   .��6�   0.006�   �� 5,      $��3��6   $��2��6%   $��Center=�65   $��Right=�6P   0 deg6s   5-      (� 5/   
   $��0��6   $��1��6   
 (� Stretch$
 Uniform=�6/    (� StretchDirection$
 Both=�6A   $��10q�6Y   .��50      �� $ ��.\Icons\StageOffsetCal.png��6   51      �� 53   
   $��1��6   $��0��6"   $��2��62    �� Orientation$ 
Horizontal=�6F   $��Right=�6_   }�. .+�54      �� -   5        $��btnDialogOk��54   &    �� 	IsDefault . $��60��6S   $��60��6^   $��0,0,10,0q�6l   .��6~   _Ok6�   �� 55       �� IsCancel . $��60��6%   $��60��60   .��6>   	_Cancel6G   56      57      58      