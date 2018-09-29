import setuptools, os, platform

windows_build = os.name == 'nt'
mac_build = platform.system() == 'Darwin'

os_classifier = "Operating System :: Microsoft :: Windows"
if mac_build:
    os_classifier = "Operating System :: MacOS"

with open("README.md", "r") as fh:
    long_description = fh.read()

print "file = " + __file__
def create_package_list(base_package):
    return ([base_package] +
            [base_package + '.' + pkg
             for pkg
             in setuptools.find_packages(base_package)
             ])

setuptools.setup(
    name="rhino3dm",
    version="0.0.2",
    author="Robert McNeel & Associates",
    author_email="steve@mcneel.com",
    description="OpenNURBS based package with a RhinoCommon style",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/mcneel/rhino3dm.py",
    packages= create_package_list("rhino3dm"),
    include_package_data=True,
    classifiers=[
        "Development Status :: 3 - Alpha",
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        os_classifier,
        "Programming Language :: Python :: 2.7",
        "Programming Language :: Python :: Implementation :: CPython"
    ],
)
