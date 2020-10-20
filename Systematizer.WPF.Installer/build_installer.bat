set PATH=%PATH%;C:\Program Files (x86)\WiX Toolset v3.11\bin
cd \stage\SystematizerPublish
candle \dev\Systematizer\Systematizer.WPF.Installer\Product.wxs
light -ext WixUIExtension Product.wixobj
pause

rem "C:\Program Files (x86)\WiX Toolset v3.11\bin\candle" Product.wxs
rem "C:\Program Files (x86)\WiX Toolset v3.11\bin\light" -ext WixUIExtension Product.wixobj
rem pause


rem cd "C:\Program Files (x86)\WiX Toolset v3.11\bin\"
rem candle C:\dev\Systematizer\Systematizer.WPF.Installer\Product.wxs
rem light -ext WixUIExtension C:\dev\Systematizer\Systematizer.WPF.Installer\Product.wixobj
rem pause
