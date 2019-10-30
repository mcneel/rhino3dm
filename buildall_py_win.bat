REM batch file used to compile all flavors for Windows.
REM Typically you would not use this file to compile and
REM instead just call python build_python_lib.py

C:\Python27\python.exe setup.py sdist
C:\Python27\python.exe setup.py bdist_wheel
C:\Python27_64bit\python.exe setup.py bdist_wheel
"C:\Users\sbaer\AppData\Local\Programs\Python\Python37\python.exe" setup.py bdist_wheel
"C:\Users\sbaer\AppData\Local\Programs\Python\Python37-32\python.exe" setup.py bdist_wheel
"C:\Users\sbaer\AppData\Local\Programs\Python\Python38\python.exe" setup.py bdist_wheel
"C:\Users\sbaer\AppData\Local\Programs\Python\Python38-32\python.exe" setup.py bdist_wheel
