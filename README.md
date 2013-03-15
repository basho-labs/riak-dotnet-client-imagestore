Description
===========

This sample application demonstrates how to store and retrieve binary image files using CorrugatedIron. It's a simple Winforms application with the sole purpose of demonstrating how to deal with binary files.

This is not a production application, it is intended to be used as a reference for other developers who wish to use CorrugatedIron to deal with binary files.

How to use the application
==========================

When the application loads it connects to Riak and lists all the keys in the default bucket, which is set to `ci_images`. Any items that it finds in that bucket are considered to be binary values which represent images. For each of the images that are found, the image is the pulled from Riak and displayed on screen.

* To change the bucket, modify the name of the bucket and click `Change`. This will clear the image list and reload.
* To add a new image, click `Browse`, select an image file, and then click `Add`. This will push the data to Riak and load the image in the image list.

A few caveats
=============

* The name of the file is used for the key when storing the value into Riak, hence if you upload something with the same file name the existing value will be overwritten.
* The app was put together very quickly, don't exect it to look nice!
* To determine MIME types for the files that are added, some Microsoft-specific code was used. Hence, this sample app might not run on Mono. However, the sample code which adds and removes files will work just fine and is completely portable.