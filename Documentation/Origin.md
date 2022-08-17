# Cosmos CMS Origin Story

The 2018 fire season was the worst in California history.  Wind-driven fires ignited by downed power lines caused fires to spread across 2 million acres. Ninety seven citizens and six fire fighters lost thier lives that year, and the fires caused over 25 billion of dollars worth in property damange.  Eighty five fatalities were due to a single fire, called the the "Camp Fire," during which the Butte County town of Paridise burnt to the ground.

## Fall 2019

Summer and fall of 2019 saw a new round of fires. Among the largest occured in mid October 2019 called the Kincade Fire. It stated near the town Geyserville in Sonoma County.  Strong winds drove the fire south forcing mass evacuations of Healdsburg, Windsor and parts of Santa Rosa. Tens of thousands of people were suddenly on the move, and thousands more worried that they might need to evacuate at any time.  Friends and family from throughout California and beyond wanted to know what was happening.

People were accessing State of California websites for information and most were using cell phone.  Everyone with a cell phone was competing for limited network bandwidth--especially in rural areas.  Web pages were slow to load and bits of information were to be found on more than one website.

Early-on three problems were recognized. First, the majority of State websites were designed for high-badwidth connections--which was not the case now.  Second, most of the websites were not built to sustain extremely high user demands, and third people had to visit multiple websites to get information.

California needed a single website that consolidated information and is designed for low-bandwidth connections and able to sustain high user loads.

## Lessons of Simplicity

In response the Governors Office and Government Operations agency tasked the Department of Technology (CDT) to setup a new website that would consolidate information about the fires and work well over limited bandwidth and on cell phones.  The deadline was to do this overnight.  Moreover this website _could not got down_ once launched.

At first CDT staff considered using WordPress or another content management system.  But these websites historically were not high performers. Their complexity also meant there more could go wrong. And setting these systems up to sustain a high load would take time and testing--time is something we did not have.  

Moreover, CDT wanted the website to be hosted simultaeously in multiple regions--and keeping these websites in sync in real time.  Setting this up would not likely happen overnight.

The decisions was to keep things simple.

CDT created a "static" website hosted simultaeously in multiple geographic regions in the cloud.  Web pages were designed without the use of unecessary graphics or other assets, making the number of bytes sent over the wire for each page was kept to a minimum.  In addition, CDT installed a Content Distribution Network or CDN to support high user loads.

This static website design offered advantages as it was highly reliable and was very fast at serving web pages, it can even sustain high user loads.

And it worked.  Overnight the site was created.  It never went down during the 2019 fire season, and it handled extremely high demand and performed very well over low bandwidth connections used by cell phones.

## Labor Intensive

While the website was fast, efficient and reliable, it did require a large number of people to maintain it around the clock.  Public information officers were the source of content and they would send information to developers who would then update web pages on the website.  Changes were pushed to the website instances using a DevOps pipleline.  To make this work teams of developers and DevOps engineers needed to work around the clock with public information officers.  In the short term this worked, but long term this was not sustainable. The realization came that some kind of a CMS would be needed in the future.  Something as reliable and as fast as a static website was needed that didn't require around the clock staffing by technical staff.

What was needed was something in between a static website and a dynamic CMS such as WordPress.

## Build Something Fast

That winter work began on a new system that would perform as well as a static website yet offered the benefits of a CMS.  What was needed was a _dynamic_ content management system that achieved the following objectives:

* Performed as well as a static website.
* Simplisitc in nature, allowing a non-techical person with little or no training update content.
* Did not require teams of developers and DevOps engineers to staff to maintain content.

From the outset it was decided to build something that was a cross between a static website and a CMS.  In other words, design something that was a little of both--build a hybrid.

### Hybrid Design

Cosmos is a hybrid website in the sense that it is part "static" website and part "dynamic."  The design uses two websites to do this.  The first is the "static" website and it hosts content that does not change very often like JavaScript, CSS, images and other files.  In a sense this retains the architecture of the original website built in 2019 that proved reliable and fast.

The second website hosts HTML web pages, and these can change frequently.  It is a dynamic website, yet is extremely simplistic in design and therefore less error prone, and it also uses short term memory cache to boost performance.  The result is that the dynamic site can serve HTML web pages 2 1/2 times faster than the static website.  Overall what this means is that Cosmos architecture performs just as a static website--and sometimes even better.

Cosmos has a third website called the "Editor" has tools to maintain content for both websites.  It contains a file manager that allows users to upload and manage files on the Storage Website.  The file manager is capabile of keeping more than one blob storage accounts in synch in real time--and across both Amazon Web Services and Microsoft Azure.

The Editor also comes with two editors.  The first for use by developers is the Monaco editor. It is the editor used in Visual Studio Code, but in this case it is running in a web browser.  HTML, JavaScript and CSS all can be edited with this editor.  The second editor is the Kendo Editor. It is known as a WYSIWYG editor, and it is for use by non-technical personnel.

Keeping the Editor separate from the other two has advantages.  The overhead and complexity of content management is isolated from the two websites that host web content. This keeps content hosting fast and reliable.

An architectual diagram of Cosmos HL is shown below.

![Image of Cosmos Architecture](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Cosmos%20HL%20Design.png)

