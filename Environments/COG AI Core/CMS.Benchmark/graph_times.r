if (!require("ggplot2")) {
  install.packages("ggplot2")
}
if (!require("reshape2")) {
  install.packages("reshape2")
}
library("ggplot2")
library("reshape2")
library(RColorBrewer)

counts <- c("3", "5", "7", "9", "16", "32", "48", "64", "48_no_limit")
titles <- counts

titles[length(titles)] = "48"

#counts <- c("5")
#titles <- counts


directory <- 'd:/tmp/cog/results/'

i = 1
for (count in counts) {
  dataFile = paste(directory, 'pgs_times_', count, '.csv', sep="")
  
  #outDir = substr(dataFile, 1, nchar(dataFile) - 4)
  outDir = paste(paste(head(unlist(strsplit(dataFile, "/")), n = -1), collapse="/"), "times", sep="/")
  if (!dir.exists(outDir)) {
    dir.create(outDir)
  }
  
  fileName <- sub(".csv", "", tail(unlist(strsplit(dataFile, "/")), n = 1))
  
  d <- read.csv(dataFile, sep = ",", stringsAsFactors = FALSE)
  
  dfm <- melt(d[,c('name', 'median_time', 'avg_time', 'avg_max_time', 'p90_max_time')],id.vars = 1)
  
  p <- ggplot(dfm, 
              aes(
                x = name,
                y = value,
                ymax = max(max(2000, dfm$value))
                )) + 
    geom_bar(aes(fill = variable), stat = "identity", position = position_dodge(0.9)) +
    scale_fill_manual(values = brewer.pal(length(unique(dfm$variable)), "Dark2")) +
    scale_y_continuous(breaks = c(100, 500, 2000), minor_breaks = NULL) +
    theme(
      panel.grid.major.x = element_blank(),
      panel.grid.major.y = element_line(colour = "#000000aa", linetype = "dashed"), 
      panel.ontop = TRUE, 
      panel.background = element_rect(fill = NA, colour="grey80")) +
    labs(x="AI type", y="Time [milliseconds]") +
    ggtitle(paste(gsub("_", " ", fileName), "vs", count, sep="")) +
    theme(
      plot.title = element_text(hjust = 0.5),
      text = element_text(size = 20, colour="black")
    ) +
    geom_text(
      size = 7,
      aes(label = round(dfm$value, digits = 0), group=variable),
      position = position_dodge(0.9),
      vjust = -0.25)
  
  ggsave(paste(outDir, paste(fileName, ".png", sep=""), sep="/"), width=14, height=7)
  i = i + 1
}