# A Data-Driven Approach to Bus Timetable Optimisation Recommendations - Jonathan Foot
### Undergraduate Dissertation - University of Notitngham, School of Computer Science.

The following GitHub repo contains all of the work I completed for my third-year undergraduate disseration. The program uses the Reading Buses Open Data API, which has since been turned off and as such the program no longer functions. However, a new API could be implimented with ease. The program takes in several days worth of hisotrical bus timetable/journey information and then optimises the timetable based upon three main critra. 

1. Minimise the unneeded slack (dwell) time at timing points and the travel times at every stop while balancing the percentage of buses predicted to be “on-time”. 
    * How likely a bus is to meet its scheduled times, given traffic and passenger demand levels.
2. Minimise the total number of changes and the severity of the changes, aiming to keep the timetable as close to the original as possible. Too many changes at once make it difficult to be able to reliably predict how it is likely to function.
    * Keep changes to a minimal to bear resemblance to original timetables.
3. Maximise the uniform distribution of different services arriving at the same stop, by spreading out headway times. This requires identifying routes that share a common route-segment.
    * Preventing bunches of buses turning up at a stop a specific times of the day. When all services share a route-segment. 

The search algorithm is implimented using Tabu-Search with Squeaky Wheel Optimisation for a more targeted approach to the search space. The code is written in C# .NET 5. For [more information, please see my website](https://www.jonathanfoot.com/Dissertation.html)


## Abstract 
In this dissertation, I explored using several months worth of historical bus journey data to feed a search algorithm on three main optimisation criteria, with the goal that the program could be used by a bus operator to aid with improving their timetables during a review period. More data than ever before is being recorded about buses within the UK and this provided an exciting opportunity to encourage greater usage of buses, for the benefits they provide.

The first optimisation criteria was minimising unneeded slack time and travel times while balancing the percentage of buses predicted to be on time. The second optimisation criteria was to maximise cohesion between services that share a common route segment, by ensuring they are more spread out at shared stops. A shared route segment is defined as "N" consecutive stops shared by two or more services, where "N" is the minimum segment length. The third optimisation target is to minimise change as much as possible, as too many changes at once make it very difficult to predict how it is likely to perform in the real world. The search algorithm used is tabu-search coupled with squeaky wheel optimisation for a more targeted and informed search. I have demonstrated that tabu-search and squeaky wheel optimisation can be used to optimise a buses timetable to great effect.
