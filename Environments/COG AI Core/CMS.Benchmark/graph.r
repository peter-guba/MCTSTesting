if (!require("ggplot2")) {
  install.packages("ggplot2")
}
if (!require("reshape2")) {
  install.packages("reshape2")
}
library("ggplot2")
library("reshape2")

countStats <- function(propTestData) {
  estim <- t(t(sapply(Map(function (x) { x[["estimate"]] }, propTestData), "[[", "p")))
  colnames(estim) <- "estimate"
  bounds <- t(sapply(propTestData, "[[","conf.int"))
  colnames(bounds) <- c("lower", "upper")
  return(data.frame(estim, bounds))
}

args = commandArgs(trailingOnly = TRUE)
if (length(args) == 0) {
  stop("At least one argument must be supplied (input file)\n")
} else if (length(args) == 1) {
  dataFile <- args[1]
}

outDir = substr(dataFile, 1, nchar(dataFile) - 4)

fileName <- sub(".csv", "", tail(unlist(strsplit(dataFile, "/")), n = 1))
p1name <- unlist(strsplit(fileName, "_vs_"))[1]
p2name <- unlist(strsplit(fileName, "_vs_"))[2]

d <- read.csv(dataFile, sep = ";", stringsAsFactors = FALSE)

# Sort by original order not alphabetically 
d$battleName <- factor(d$battleName, levels = d$battleName)

# Calculate remaining data
d$p1winRate <- countStats(Map(prop.test, x = d$p1win, n = (2 * d$repeats)))
d$p2winRate <- countStats(Map(prop.test, x = d$p2win, n = (2 * d$repeats)))
d$p1symWinRate <- countStats(Map(prop.test, x = d$p1symWin, n = d$repeats))
d$p2symWinRate <- countStats(Map(prop.test, x = d$p2symWin, n = d$repeats))

makeWinRateGraph <- function(yLabel, d, yData, sym=FALSE) {
  symStr = ""
  if (sym) {
    symStr = "symmetric"
  }
  ggplot(data = d, aes(
    x = battleName, 
    y = yData$estimate,
    ymin = yData$lower,
    ymax = yData$upper,
    fill = battleName,
    label = sprintf("%0.2f", round(yData$estimate, digits = 2)))) +
  geom_bar(colour="black", size=.3, stat="identity", position=position_dodge()) +
  geom_errorbar() + 
  scale_y_continuous(limits = c(0, 1)) +
  geom_text(size = 6, hjust = 0.5, vjust=1) +
  guides(fill=FALSE) +
  labs(x="Scenario", y=paste(yLabel, "winrate", symStr, sep=" ")) +
  ggtitle(sub("_vs_", " vs ", fileName)) +
  theme(plot.title = element_text(hjust = 0.5))
}

# Plot graphs
p1winRatePlot <- makeWinRateGraph(p1name, d, d$p1winRate)
ggsave(paste(outDir, "p1winRates.png", sep="/"), width=14, height=7)
p2winRatePlot <- makeWinRateGraph(p2name, d, d$p2winRate)
ggsave(paste(outDir, "p2winRates.png", sep="/"), width=14, height=7)

p1symWinRatePlot <- makeWinRateGraph(p1name, d, d$p1symWinRate, TRUE)
ggsave(paste(outDir, "p1symWinRates.png", sep="/"), width=14, height=7)
p2symWinRatePlot <- makeWinRateGraph(p2name, d, d$p2symWinRate, TRUE)
ggsave(paste(outDir, "p2symWinRates.png", sep="/"), width=14, height=7)

roundCounts <- d[,c("battleName", "minRounds", "medianRounds", "maxRounds")]
dataMelt <- melt(roundCounts, i.vars='battleName')

roundCountPlot <- ggplot(data=dataMelt, aes(x=battleName, y=value, fill=variable)) +
  geom_bar(colour="black", size=.3, stat="identity", position=position_dodge()) +
  scale_fill_discrete(labels=c("Min", "Median", "Max")) +
  labs(x="Scenario", y="Round count") +
  ggtitle(sub("_vs_", " vs ", fileName)) +
  theme(plot.title = element_text(hjust = 0.5), legend.title=element_blank())

ggsave(paste(outDir, "roundCounts.png", sep="/"), width=14, height=7)
