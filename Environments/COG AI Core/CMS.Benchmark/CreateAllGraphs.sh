#!/bin/bash

RESULTS_DIR=bin/Release/results
FORCE=false

while getopts "f" opt; do
  case $opt in
    f)
      FORCE=true
      ;;
  esac
done

for benchSet in $RESULTS_DIR/* ; do 
  for date in $benchSet/* ; do
    for file in $date/*.csv ; do
      dir=`echo $file | sed 's/\.csv//'`
      if [ ! -d $dir ] || [ $FORCE == true ]; then
        if [ -d $dir ]; then rm -rf $dir; fi
        mkdir $dir
        Rscript graph.r $file
      fi
    done
  done
done

