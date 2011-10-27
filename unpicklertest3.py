# This program runs a batch of tests against Pyrolite's unpickler.
# (this is the Python 3.x version)
# -*- coding: UTF-8 -*-

from __future__ import print_function
import subprocess
import pickle
import datetime
import array
import os
import sys
import decimal

if sys.version_info<(3,0):
    raise RuntimeError("this test script is for Python 3.x, use the picklertest script with python 2.x")


# PYTHON TYPE --> JAVA TYPE
# None            null
# bool            boolean
# int             int
# long            Number: long or BigInteger
# string          String
# unicode         String
# complex         objects.ComplexNumber
# datetime.date   Calendar
# datetime.datetime Calendar
# datetime.time   objects.Time
# datetime.timedelta objects.TimeDelta
# float           double
# array           array
# list            ArrayList<Object>
# tuple           Object[]
# set             Set
# dict            Map
# bytearray       byte[]
# bytes           byte[]      # only exists in python3
 
datething=datetime.datetime(1999,12,31, 14,33,59, 456111)
timedelta=datetime.datetime(2011,7,18, 10,10,59, 999222)-datething

# define a few objects that are used in memo'd pickles
i1=111
i2=222
i3=333
s1="abc"
s2="def"
s3="ghi"
a1=[i1,i1]
a2=[i2,i2]
a3=[i3,i3]
ar=['a','b','X']
ar[2]=ar  # recursive

pickledata= {
    "null object": [
        (None, "null")
        ],
    "java.lang.Boolean": [
        (True, "true"),
        (False, "false"),
        ],
    "String": [
        ("", ""),
        ("hello", "hello"),
        ("hello"+chr(255), "hello"+chr(255)),
        ("hello\u20ac", "hello\u20ac")
        ],
    "net.razorvine.pickle.objects.ComplexNumber": [
        (0+0j, "0.0+0.0i"),
        (1.2-3.4j, "1.2-3.4i"),
        ],
    "java.util.Calendar": [
        (datething, "31-Dec-1999 14:33:59 millisec=456"),
        (datething.date(), "31-Dec-1999 00:00:00 millisec=0"),
        ],
    "net.razorvine.pickle.objects.Time": [
        (datething.time(), "Time: 14 hours, 33 minutes, 59 seconds, 456111 microseconds"),
        ],
    "net.razorvine.pickle.objects.TimeDelta": [
        (timedelta, "Timedelta: 4216 days, 70620 seconds, 543111 microseconds (total: 364333020.543111 seconds)"),
        ],
    "java.lang.Integer": [
        (0, "0"),
        (1, "1"),
        (127, "127"),
        (128, "128"),
        (255, "255"),
        (256, "256"),
        (30000, "30000"),
        (-30000, "-30000"),
        (60000, "60000"),
        (999999999, "999999999"),
        (-999999999, "-999999999"),
        ],
    "java.lang.Long": [
        (9999999999, "9999999999"),
        (-9999999999, "-9999999999"),
        (19999999999, "19999999999"),
        (-19999999999, "-19999999999"),
        ],
    "java.math.BigInteger": [
        (1111222233334444555566667777888899990000, "1111222233334444555566667777888899990000"),
        ],
    "java.math.BigDecimal": [
        (decimal.Decimal("123456789.987654321"), "123456789.987654321"),
        ],
    "java.lang.Double": [
        (1234.5678, "1234.5678"),
        (-1.23456e255, "-1.23456E255")
        ],
     "array of class java.lang.Object": [
        ((), "[]"),
        ((1,2), "[1, 2]"),
        (('foo','bar',99,(1,2,3),None),    "[foo, bar, 99, [1, 2, 3], null]"),
        ((s1,s1,s1), "[abc, abc, abc]"),
        ],
     "array of char": [
        (array.array('u',"abc\u20ac"), "[a, b, c, \u20ac]"),
        ],
     "array of byte": [
        (array.array('b',[1,2,-128]), "[1, 2, -128]"),
        (bytearray([1,2,3]), "[1, 2, 3]"),
        ],
     "array of short": [
        (array.array('B',[1,2,255]), "[1, 2, 255]"),
        (array.array('h',[1,2,-999]), "[1, 2, -999]"),
        ],
     "array of int": [
        (array.array('H',[1,2,65535]), "[1, 2, 65535]"),
        (array.array('i',[1,2,3]), "[1, 2, 3]"),
        (array.array('l',[1,2,-999]), "[1, 2, -999]"),
        ],
     "array of long": [
        (array.array('I',[1,2,999]), "[1, 2, 999]"),
        (array.array('L',[1,2,999]), "[1, 2, 999]"),
        ],
     "array of float": [
        (array.array('f',[1.0,2.0,3.0]), "[1.0, 2.0, 3.0]"),
        ],
     "array of double": [
        (array.array('d',[1.1,2.2,3.3]), "[1.1, 2.2, 3.3]"),
        ],
     "java.util.ArrayList": [
        ([1,2,3], "[1, 2, 3]"),
        (['a','b','c'], "[a, b, c]"),
        ([s1,s1,s1], "[abc, abc, abc]"),
        ([i1,i2,i3,i1,i2,i3], "[111, 222, 333, 111, 222, 333]"),
        ([a1,a2,a3,a1,a2,a3], "[[111, 111], [222, 222], [333, 333], [111, 111], [222, 222], [333, 333]]"),
        (ar, "[a, b, (this Collection)]"),
        ( [{1:2, 3:4}, {5:6, 7:8}, {9:10, 11:12}], "[{1=2, 3=4}, {5=6, 7=8}, {9=10, 11=12}]"),
        ],
     "java.util.HashSet": [
        (set([1,2,3,2,3]), "[1, 2, 3]"),
        (set(), "[]"),
        ],
     "java.util.HashMap": [
        ({"a":1, "b":2}, "[a=1, b=2]"),
        ({}, "[]"),
        ({s1: {}}, "[abc={}]"),
        ({s1: {s2: a1}}, "[abc={def=[111, 111]}]"),
        ({s1: {s2: a1, s3: a2}}, "[abc={def=[111, 111], ghi=[222, 222]}]"),
        ({s1: {s2: a1, s1: a1}}, "[abc={def=[111, 111], abc=[111, 111]}]"),
        ],
     "array of byte": [
        (bytearray([65,66,67]), "[65, 66, 67]"),
        (b'ABC',"[65, 66, 67]"),
        ],
    }


picklefile="pickledata.txt"
resultfile="resultdata.txt"

total_errors=0
for protocol in range(0,pickle.HIGHEST_PROTOCOL+1): 
    print("")
    print("PICKLE PROTOCOL:",protocol)
    errors=0
    data=iter(pickledata)
    for checkclass, items in pickledata.items():
        print("CHECK CLASS:",checkclass)
        for element, checkstr in items:
            print("testing {0} ".format(repr(element)), end='')
            with open(picklefile,"wb") as f:
                pickle.dump(element, f, protocol=protocol)
            if os.path.exists(resultfile):
                os.remove(resultfile)
            subprocess.check_call(["java","-classpath","build/pyrolite.jar","-Xint","net.razorvine.pickle.Unpickler",picklefile,resultfile])
            if not os.path.exists(resultfile):
                print("No outputfile. Skipping to next test.\n")
                errors+=1
                continue
            java=open(resultfile,"r", encoding="UTF-8").read()
            resultclass, resultstr = java.splitlines()
            if resultclass.startswith("class "):
                resultclass=resultclass[6:]
            ok=True
            if resultclass!=checkclass:
                print("  TYPE ERROR, resultclass={0} expected {1}".format(resultclass,checkclass))
                ok=False
            if resultstr!=checkstr:
                print("  DATA ERROR, resultstr={0} expected {1}".format(repr(resultstr),repr(checkstr)))
                ok=False
            print("")
            if not ok:
                errors+=1
    print("number of errors for protocol {0}: {1}".format(protocol, errors))
    total_errors+=errors

print("")
print("Total number of errors:",total_errors)
os.remove(picklefile)
if os.path.exists(resultfile):
    os.remove(resultfile)
